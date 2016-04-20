using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using JobSearch.Classes;
using JobSearch.Classes.Filter;
using JobSearch.Controls;
using Logger;
using Logger.AsyncProcess;
using Logger.Utils;
using Utils.Contracts.Patterns;
using Utils.Controls;
using Utils.Serialization;
using Logger.Controls;

namespace JobSearch
{
    public partial class MainForm : Form, IStreamSerializable, IModified 
    {
        private readonly JobSearcher jobSearcher;
        private readonly FormStateSaver formStateSaver;
        private readonly AsyncProcessor _asyncProcessorr;
        private readonly DeferredAsyncProcessor _deferredAsyncProcessor;
        private readonly DeferredAction _updateStatusDeferredAction;

        public MainForm()
        {
            InitializeComponent();
            if (DesignMode || AppRunMode.DesignMode) return;

            _updateStatusDeferredAction = new DeferredAction(updateStatus) { MinNextActionTime = TimeSpan.FromSeconds(1), ReExecuteOnAbortSuspended = false, ReExecuteOnError = false };

            _asyncProcessorr = new AsyncProcessor(5);
            _asyncProcessorr.ActionBegin += _asyncProcessorr_ActionsChanged;
            _asyncProcessorr.ActionEnd += _asyncProcessorr_ActionsChanged;
            _asyncProcessorr.ActionError += _asyncProcessorr_ActionsChanged;

            jobSearcher = new JobSearcher(_asyncProcessorr);
            formStateSaver = new FormStateSaver(this, true, "Reserve");
            jobSearcher.Modified += jobSearcher_Modified;

            _deferredAsyncProcessor = new DeferredAsyncProcessor(new AsyncProcessor(10));

            objectEditor1.DeferredAsyncProcessor = _deferredAsyncProcessor;
            resultList1.DeferredAsyncProcessor = _deferredAsyncProcessor;
            resultList2.DeferredAsyncProcessor = _deferredAsyncProcessor;
            resultList3.DeferredAsyncProcessor = _deferredAsyncProcessor;
            resultList4.DeferredAsyncProcessor = _deferredAsyncProcessor;

            resultList1.JobSearcher = jobSearcher;
            resultList2.JobSearcher = jobSearcher;
            resultList3.JobSearcher = jobSearcher;
            resultList4.JobSearcher = jobSearcher;

            resultList1.Results = jobSearcher.Results;
            resultList2.Results = jobSearcher.Favorites;
            resultList3.Results = jobSearcher.Favorites2;
            resultList4.Results = jobSearcher.BlockedResults;

            resultList1.BlockCompanies += resultList1_BlockCompanies;
            resultList2.BlockCompanies += resultList1_BlockCompanies;
            resultList3.BlockCompanies += resultList1_BlockCompanies;
            resultList4.BlockCompanies += resultList1_BlockCompanies;

            IDictionaryChangedDict<ICollectionChangedList<Result>, string> resultsDict = new SortDict<ICollectionChangedList<Result>, string>(CompareUtils.CompareHashCode);
            resultList1.ResultsDict = resultsDict;
            resultsDict.Add(jobSearcher.Results, "Results");
            resultList2.ResultsDict = resultsDict;
            resultsDict.Add(jobSearcher.Favorites, "Favorites");
            resultList3.ResultsDict = resultsDict;
            resultsDict.Add(jobSearcher.Favorites2, "Not sure");
            resultsDict.Add(jobSearcher.BlockedResults, "Blocked");
            resultList4.ResultsDict = resultsDict;

            objectEditor1.SetBindObject(jobSearcher.Sites);

            checkTextBox1.CheckTextChanged += checkTextBox1_CheckTextChanged;
            checkTextBox2.CheckTextChanged += checkTextBox1_CheckTextChanged;

        }

        void _asyncProcessorr_ActionsChanged(object sender, ThreadActionEventArgs e)
        {
            _updateStatusDeferredAction.Execute();
        }

        void checkTextBox1_CheckTextChanged(object sender, EventArgs e)
        {
            OnModified(false);
        }

        private void updateStatus()
        {
            this.TryInvoke((Action)(() =>
            {
                searchToolStripMenuItem.Enabled = _asyncProcessorr.ActiveActionsCount + _asyncProcessorr.PassiveActionsCount == 0;
                toolStripStatusLabel2.Text = (_asyncProcessorr.PassiveActionsCount + _asyncProcessorr.ActiveActionsCount).ToString();
                toolStripStatusLabel4.Text = (_asyncProcessorr.ActiveActionsCount).ToString();
            }));
        }

        void jobSearcher_Modified(object sender, EventArgs e)
        {
            OnModified(false);
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            formStateSaver.Save(true);
            var filters = new SortedList<StringMatchFilter>(false, false);
            filters.AddCollection(getFilters(checkTextBox1));
            filters.AddCollection(getCompanyFilters(checkTextBox2));
            jobSearcher.Filters = filters;
            jobSearcher.Search();
            updateStatus();
        }

        private readonly Regex _filterRegEx = new Regex(@"([\+\-])(\w+)([\+\-])(.*)$");

        private IList<StringMatchFilter> getFilters(CheckTextBox checkTextBox)
        {
            var lines = checkTextBox.GetLines(true, false);
            var len = lines.Count;
            var filters = new List<StringMatchFilter>(len + 1);
            for (int i = 0; i < len; i++)
            {
                var line = lines[i];
                if (String.IsNullOrEmpty(line)) continue;
                var match = _filterRegEx.Match(line);
                if (!match.Success)
                {
                    Log.Add(RecType.UserError, "Incorrect line: " + line + "\r\nMust be \"(+/-)(+/-)(Name|Content|Company)(+/-)(Regexp)\" for example: \"++Content=C#|\\.Net\"");
                    continue;
                }
                var pattern = match.Groups[4].Value;
                filters.Add(new Filter(pattern, match.Groups[3].Value == "-", match.Groups[2].Value, match.Groups[1].Value == "+" ? FilterPermission.Allow : FilterPermission.Deny, FilterSearchType.Regex));
            }
            return filters;
        }

        private IList<StringMatchFilter> getCompanyFilters(CheckTextBox checkTextBox)
        {
            var lines = checkTextBox.GetLines(true, false);
            var len = lines.Count;
            var filters = new List<StringMatchFilter>(len + 1);
            for (int i = 0; i < len; i++)
            {
                var line = lines[i];
                if (String.IsNullOrEmpty(line)) continue;
                var @deny = line[0] == '-';
                if (@deny || line[0] == '+') line = line.Substring(1, line.Length - 1);
                var pattern = line.ToLower();
                pattern = new Regex(@"[\W_]+", RegexOptions.IgnoreCase).Replace(pattern, @"[\W_]+");
                filters.Add(new Filter(@"^[\W_]*" + pattern + @"[\W_]*$", false, "Company", @deny ? FilterPermission.Deny : FilterPermission.Allow, FilterSearchType.Regex));
            }
            return filters;
        }

        private readonly int currentVersion = 2;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(currentVersion);
            checkTextBox1.Serialize(writer);
            checkTextBox2.Serialize(writer);
            jobSearcher.Serialize(writer);
        }

        public object DeSerialize(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            checkTextBox1.DeSerialize(reader);
            checkTextBox2.DeSerialize(reader);
            if (version == 1) new CheckTextBox().DeSerialize(reader);
            jobSearcher.DeSerialize(reader);
            return this;
        }

        public event EventHandler Modified;

        public void OnModified(bool newThread)
        {
            formStateSaver.Save(false);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.SaveToFile(saveFileDialog1.FileName);
            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.LoadFromFile(openFileDialog1.FileName);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _deferredAsyncProcessor.ForceDeferredExecute();
        }

        private void addCompaniesToFilter(ILockerList<Result> results)
        {
            lock (results.Locker)
            {
                var sb = new StringBuilder(checkTextBox2.Text);
                foreach (var result in results)
                {
                    if (!result.Selected) continue;
                    if (sb.Length > 0) sb.AppendLine();
                    sb.Append("+-").Append(result.Company);
                }
                checkTextBox2.Text = sb.ToString();

                var filters = new SortedList<StringMatchFilter>(false, false);
                filters.Add(new Filter(".*", false, "Name", FilterPermission.Allow, FilterSearchType.Regex));
                filters.AddCollection(getCompanyFilters(checkTextBox2));
                jobSearcher.Filters = filters;
                jobSearcher.ReFilter();
            }
        }

        private void resultList1_BlockCompanies(object sender, EventArgs e)
        {
            var resultList = (ResultList) sender;
            addCompaniesToFilter(resultList.Results);
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _asyncProcessorr.AbortAllActions(0);
        }
    }
}
