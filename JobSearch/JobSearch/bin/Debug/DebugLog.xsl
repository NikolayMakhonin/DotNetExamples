<?xml version="1.0"?>

<!DOCTYPE xsl:stylesheet[
  <!ENTITY nbsp "&#160;">
  <!ENTITY lt "&#60;">
  <!ENTITY gt "&#62;">
  <!ENTITY copy "&#169;">
  <!ENTITY amp "&#38;">
  <!ENTITY raquo "&#187;">
  <!ENTITY laquo "&#171;">
]>
<xsl:stylesheet version="2.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="xml" encoding="utf-8" />
  <xsl:template match="/">
    <html>
      <head>
        <meta http-equiv='Content-Type' content='text/html; charset=utf-8' />
        <style type="text/css">
          .Process{color:#000000}
          .Action{color:#0000FF}
          .UserAction{color:#0000FF}
          .UserError{color:#FF4040}
          .UserWarning{color:#BF0000}
          .Error{color:#FF0000}
          .Fatal{color:#FF0000}
          .Info{color:#7F7F7F}
          .None{color:#000000}
          .Warning{color:#BF0000}
          .Contract{color:#BF0000}
          p, pre{margin-bottom: 0; margin-top: 0}
          .exception{color:#FF0000; font-size: 8pt}
          .stacktrace{color:#7F7F7F; font-size: 8pt}
          A:link {text-decoration: none}
          A:hover {text-decoration: none}
          .logMessageCollapse { height: 17px; overflow: hidden; margin-top: -17px; }
          .logMessageExpand { height: auto; overflow: hidden; margin-top: -17px; }
          .logMessageHeader { height: 17px; width: 10000px; background-color: #f0f0ff; border-top-style: solid; border-top-width: 1px; border-top-color: #7f7f7f; }
          .logMessageContainer { width: 100%; position: relative; }
        </style>
        <script type="text/javascript">
          function logMessageExpandCollapse(elem)
          {
          elem.parentElement.parentElement.parentElement.className=(elem.parentElement.parentElement.parentElement.className=='logMessageCollapse')?'logMessageExpand':'logMessageCollapse';
          }

          var WshShell = null;
          try
          {
            WshShell = new ActiveXObject("WScript.Shell");
          }
          catch (e)
          {
            WshShell = null;
          }
          
          function Run(file)
          {
            try
            {
              file = file.replace(/file:\/\/\//, "");
              file = file.replace(/file:\/\/localhost\//, "");
              file = decodeURIComponent(file);
              file = file.replace(/\//, "\\");
              WshShell.Exec("cmd /C \"" + file + "\"");
              return true;
            }
            catch (e)
            {
              return false;
            }
          }
          
          function OnAnchorClick() 
          {
            Run(this.href);
          }
          
          function InitAnchors() 
          {
            var anchors = document.getElementsByTagName("a")
            if (!anchors) return;
            for (var key in anchors) {
                var anchor = anchors [key];
                if (WshShell)
                {
                  anchor.target = "_blank";
                  anchor.onclick = OnAnchorClick;
                }
                else
                {
                  anchor.target = "_self";
                }
            }          
          }

          document.onload = function () 
          {
             InitAnchors();
          }
          
          document.onreadystatechange = function () 
          {
            if (document.readyState == "complete") InitAnchors();
          }
          
          document.onerror = function () 
          {
            InitAnchors();
          }
          
          document.onabort = function () 
          {
            InitAnchors();
          }
        </script>
      </head>
      <div style="width: 100%; font-family: 'Courier New'; font-size: 10pt;">
        <xsl:for-each select="/root/LogUrls/LogUrl">
          <xsl:element name="u">
            <xsl:element name="a">
              <xsl:attribute name="href">
                  <xsl:value-of select="."/>
              </xsl:attribute>
              <xsl:value-of select="position() - 1"/>
            </xsl:element>
          </xsl:element>
          <xsl:text> | </xsl:text>
        </xsl:for-each>
      </div>
      <hr/>
      <span style="font-family: 'Courier New'; font-size: 10pt;">
        <xsl:for-each select="/root/event">
          <xsl:sort order="descending" select="date"/>
          <div class="logMessageContainer">
            <div class="logMessageHeader">&nbsp;</div>
            <div class="logMessageCollapse">
              <!-- DateTime to str: http://www.w3.org/TR/xslt20/#date-time-examples -->
              <pre class="{rectype} firstRow"><b><span onclick="logMessageExpandCollapse(this)">[<xsl:value-of select="msxsl:format-date(date, 'dd.MM.yyyy ')"/> <xsl:value-of select="msxsl:format-time(date, 'HH:mm:ss')"/>][<xsl:value-of select="rectype"/>][<xsl:value-of select="exceptionType" disable-output-escaping="yes"/>][<xsl:value-of select="methodName" disable-output-escaping="yes"/>][<xsl:value-of select="ip"/>]:</span></b><b><xsl:value-of select="message" disable-output-escaping="yes"/></b></pre>
              <br/>
              <xsl:if test="string-length(assemblyName)&gt;2">
                  <xsl:if test="string-length(assemblyVersion)&gt;2">
                      <pre class="exception">[Assembly: <xsl:value-of select="assemblyName"/>.<xsl:value-of select="assemblyVersion"/>]</pre>
                  </xsl:if>
              </xsl:if>
              <xsl:if test="string-length(ip)&gt;2">
                <pre class="exception">IP = <xsl:value-of select="ip" disable-output-escaping="yes"/></pre>
              </xsl:if>
              <xsl:if test="string-length(machineName)&gt;2">
                <pre class="exception">MachineName = <xsl:value-of select="machineName" disable-output-escaping="yes"/></pre>
              </xsl:if>
              <xsl:if test="string-length(url)&gt;2">
                <pre class="exception">Url = <xsl:value-of select="url" disable-output-escaping="yes"/></pre>
              </xsl:if>
              <xsl:if test="string-length(urlReferrer)&gt;2">
                <pre class="exception">UrlReferrer = <xsl:value-of select="urlReferrer" disable-output-escaping="yes"/></pre>
              </xsl:if>
              <xsl:if test="string-length(exception)&gt;5">
                <pre class="exception"><xsl:value-of select="exception" disable-output-escaping="yes"/></pre>
              </xsl:if>
              <xsl:if test="string-length(logstacktrace)&gt;5">
                <pre class="stacktrace">LogStackTrace:<xsl:value-of select="logstacktrace" disable-output-escaping="yes"/></pre>
              </xsl:if>
              <xsl:if test="string-length(threadstacktrace)&gt;5">
                <pre class="stacktrace">ThreadStackTrace:<xsl:value-of select="threadstacktrace" disable-output-escaping="yes"/></pre>
              </xsl:if>
              <xsl:if test="string-length(customstacktrace)&gt;5">
                <pre class="stacktrace">CustomStackTrace:<xsl:value-of select="customstacktrace" disable-output-escaping="yes"/></pre>
              </xsl:if>
              <br/>
            </div>
          </div>
        </xsl:for-each>
      </span>
      <hr/>
      <div style="width: 100%; font-family: 'Courier New'; font-size: 10pt;">
        <xsl:for-each select="/root/LogUrls/LogUrl">
          <xsl:element name="u">
            <xsl:element name="a">
              <xsl:attribute name="href">
                  <xsl:value-of select="."/>
              </xsl:attribute>
              <xsl:value-of select="position() - 1"/>
            </xsl:element>
          </xsl:element>
          <xsl:text> | </xsl:text>
        </xsl:for-each>
      </div>
    </html>
  </xsl:template>
</xsl:stylesheet>
