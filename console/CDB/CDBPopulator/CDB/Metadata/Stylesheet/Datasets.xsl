<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet
  version="1.1"
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:cdb="http://www.opengis.net/cdb/1.2/Datasets"
  xmlns="http://www.w3.org/1999/xhtml"
  xmlns:html="http://www.w3.org/1999/xhtml" >

  <xsl:output method="html" version="4.0" indent="yes"/>

  <xsl:template match="/">
    <html>
      <head>
        <title>OGC CDB 1.2 Datasets</title>
      </head>
      <body>
        <xsl:apply-templates/>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="cdb:Datasets">
    <h1>OGC CDB 1.2 Datasets</h1>
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="cdb:Dataset">
    <h2>
      Dataset <xsl:value-of select="@code"/>, <xsl:value-of select="@name"/>
      <xsl:if test="@deprecated = true()">
        <xsl:text> </xsl:text>
        (deprecated)
      </xsl:if>
    </h2>
    <ul>
      <xsl:apply-templates/>
    </ul>
  </xsl:template>

  <xsl:template match="cdb:Encoding">
    <xsl:apply-templates/>
  </xsl:template>

  <xsl:template match="cdb:Format">
    <li>
      Encoding Format:
      <xsl:value-of select="@name"/>
      <xsl:if test="@version">
        version <xsl:value-of select="@version"/>
      </xsl:if>
      <xsl:apply-templates/>
    </li>
  </xsl:template>

  <xsl:template match="cdb:File">
    <li>
      File Extension: <xsl:value-of select="@extension"/>
    </li>
  </xsl:template>

  <xsl:template match="cdb:Component">
    <li>
      Component <xsl:value-of select="@code"/>, <xsl:value-of select="@name"/>
      <xsl:if test="boolean(@deprecated)">
        <xsl:text> </xsl:text>
        (deprecated)
      </xsl:if>
    </li>
  </xsl:template>

</xsl:stylesheet>
