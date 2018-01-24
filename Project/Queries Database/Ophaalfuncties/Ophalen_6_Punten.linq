<Query Kind="SQL">
  <Connection>
    <ID>e45b3fd1-d228-48bd-a289-a98ea64b00f7</ID>
    <Persist>true</Persist>
    <Server>.\</Server>
    <Database>TCH_HAHO_DEL</Database>
    <ShowServer>true</ShowServer>
  </Connection>
</Query>

SELECT [Punt_ID]
      ,[Project]
      ,[Onderstation]
      ,[Volgnummer]
      ,[Label]
      ,[Omschrijving]
      
  FROM [dbo].[Punten] 
  ORDER BY Omschrijving ASC
  




