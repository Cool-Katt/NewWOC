# Files in this directory are queries that are sent to the DB to generate Work Orders

------

## The file naming convention is as follows:
`(POSITION)(TAG)(TAG)(TAG)---Whatever_Name_You_Want.sql`
1. tags are separated by parenthesis
2. the first tag should **ALWAYS** be the `POSITION` tag, signifying the sheet order.
3. `POSITION` tag is a number,  between *00* and *99*
4. there is no limit to the number of tags you can use
5. tags **must be** uppercase only
6. each tag can **ONLY** be one of the following:
   * (GSM)
   * (GSM_GL)
   * (UMTS)
   * (LTE)
   * (ALL)
   * (ALL_GL)
7. the file name is separated from the tags by 3 dashes `---`
8. spaces in the file name are **NOT** allowed
