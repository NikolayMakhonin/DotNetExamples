using System;
using System.ComponentModel;
using System.Windows.Forms;
using JobSearch.Classes;
using Logger.AsyncProcess;
using Logger.Utils;
using Utils.Controls;

namespace JobSearch.Controls
{
    public partial class ResultList : UserControl
    {
        private JobSearcher _jobSearcher;
        private ICollectionChangedList<Result> _results;
        private IDictionaryChangedDict<ICollectionChangedList<Result>, string> _resultsDict;
        private readonly IDictionaryChangedDict<ICollectionChangedList<Result>, ToolStripMenuItem> _resultsToMenuItem;
        private readonly AllControlsEvents allSubControlsEvents = new AllControlsEvents(new AllSubControls());

        public ResultList()
        {
            InitializeComponent();
            _resultsToMenuItem = new SortDict<ICollectionChangedList<Result>, ToolStripMenuItem>(CompareUtils.CompareHashCode);
            objectEditor1.KeyUp += objectEditor1_KeyUp;
            allSubControlsEvents.BindEvents += allSubControlsEvents_BindEvents;
            allSubControlsEvents.UnBindEvents += allSubControlsEvents_UnBindEvents;
            allSubControlsEvents.Init(objectEditor1);
        }

        void allSubControlsEvents_BindEvents(object sender, GlobalControlEventsArgs e)
        {
            e.Control.KeyUp += objectEditor1_KeyUp;
        }

        void allSubControlsEvents_UnBindEvents(object sender, GlobalControlEventsArgs e)
        {
            e.Control.KeyUp -= objectEditor1_KeyUp;
        }

        void objectEditor1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control || e.Shift || e.Alt)
            {
                if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.A)
                {
                                        
                }
            }
            else
            {
                if (e.KeyCode == Keys.Space) {
                    var selectedItems = objectEditor1.GetSelectedRows<Result>();
                    var len = selectedItems.Count;
                    if (len > 0)
                    {
                        var isSelected = true;
                        for (int i = 0; i < len; i++)
                        {
                            if (!selectedItems[i].Selected)
                            {
                                isSelected = false;
                                break;
                            }
                        }

                        for (int i = 0; i < len; i++)
                        {
                            selectedItems[i].Selected = !isSelected;
                        }
                        objectEditor1.UpdateSelectedRows();
                        e.Handled = true;
                    }
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public JobSearcher JobSearcher
        {
            get { return _jobSearcher; }
            set { _jobSearcher = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IDeferredAsyncProcessor DeferredAsyncProcessor
        {
            get { return objectEditor1.DeferredAsyncProcessor; }
            set { objectEditor1.DeferredAsyncProcessor = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ICollectionChangedList<Result> Results
        {
            get { return _results; }
            set
            {
                _results = value;
                unBindResultsList(_results);
                objectEditor1.SetBindObject(_results);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IDictionaryChangedDict<ICollectionChangedList<Result>, string> ResultsDict
        {
            get { return _resultsDict; }
            set
            {
                if (_resultsDict == value) return;
                if (_resultsDict != null)
                {
                    _resultsDict.DictionaryChanged -= _resultsDict_DictionaryChanged;
                    foreach (var item in _resultsDict)
                    {
                        unBindResultsList(item.Key);
                    }
                }
                _resultsDict = value;
                if (_resultsDict != null)
                {
                    foreach (var item in _resultsDict)
                    {
                        bindResultsList(item.Key, item.Value);
                    }
                    _resultsDict.DictionaryChanged += _resultsDict_DictionaryChanged;
                }
            }
        }

        private void bindResultsList(ICollectionChangedList<Result> results, string name)
        {
            if (results == null || results == _results) return;
            var menuItem = new ToolStripMenuItem(name);
            menuItem.Click += (sender, args) =>
            {
                if (_jobSearcher == null || _results == null) return;
                _jobSearcher.MoveResultsTo(_results, results);
            };
            moveToToolStripMenuItem.DropDownItems.Add(menuItem);
            _resultsToMenuItem[results] = menuItem;
        }

        private void unBindResultsList(ICollectionChangedList<Result> results)
        {
            var menuItem = _resultsToMenuItem[results];
            if (menuItem == null) return;
            moveToToolStripMenuItem.DropDownItems.Remove(menuItem);
            menuItem.Dispose();
            _resultsToMenuItem.Remove(results);
        }

        void _resultsDict_DictionaryChanged(object sender, DictionaryChangedEventArgs<ICollectionChangedList<Result>, string> e)
        {
            switch (e.ChangedType)
            {
                case DictionaryChangedType.Added:
                    bindResultsList(e.Key, e.NewValue);
                    break;
                case DictionaryChangedType.Removed:
                    unBindResultsList(e.Key);
                    break;
                case DictionaryChangedType.Setted:
                    unBindResultsList(e.Key);
                    bindResultsList(e.Key, e.NewValue);
                    break;
            }
        }

        private void openInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_jobSearcher == null || _results == null) return;
            _jobSearcher.OpenInBrowser(_results);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_jobSearcher == null || _results == null) return;
            if (MessageBox.Show("Do you want to delete selected items?", "Delete items", MessageBoxButtons.OKCancel) ==
                DialogResult.OK)
            {
                _jobSearcher.DeleteSelected(_results);
            }
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_jobSearcher == null || _results == null) return;
            _jobSearcher.SelectAll(_results);
        }

        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_jobSearcher == null || _results == null) return;
            _jobSearcher.SelectNone(_results);
        }

        private void blockCompaniesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_jobSearcher == null || _results == null) return;
            if (BlockCompanies != null) BlockCompanies(this, EventArgs.Empty);
        }

        public event EventHandler BlockCompanies;
    }
}
