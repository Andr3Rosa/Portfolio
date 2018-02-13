<Query Kind="SQL">
  <Connection>
    <ID>e45b3fd1-d228-48bd-a289-a98ea64b00f7</ID>
    <Persist>true</Persist>
    <Server>.\</Server>
    <Database>TCH_HAHO_DEL</Database>
    <ShowServer>true</ShowServer>
  </Connection>
</Query>

USE [TCH_HAHO_DEL]
GO


 

SELECT [Systeemtijd]
      ,[Waarde]
      ,[AangevuldeData]
      ,[DataGemist]
      ,[IntervalGewijzigd]
  FROM [dbo].[Haho_del_OS1001_GRFSYS_100]


WHERE Systeemtijd BETWEEN '2016-11-07' AND '2017-11-05'
  
  
GO
