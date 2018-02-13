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
  FROM [dbo].[Haho_del_OS409_GRFDBX_387]
  
  Order BY Systeemtijd ASC 
  
GO