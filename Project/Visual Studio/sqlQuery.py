import pypyodbc as od
import datetime
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import plotly.plotly as py
from plotly.graph_objs import *

#Database Connection
cnxn = od.connect("Driver={SQL Server};"
                      "Server=.\;"
                      "Database=TCH_HAHO_DEL;"
                      "Trusted_Connection=yes;")

cursor = cnxn.cursor()

#Definition Query
query = ("SELECT Systeemtijd, Waarde FROM Haho_del_OS1017_GRFSYS_13 ORDER BY Systeemtijd")

#Run SqlCommand
cursor.execute(query)

#Get data and save to list
Data = cursor.fetchall()

#rewrite columns name in table
header=['Systeemtijd','Waarde']
Data=np.array(Data)

#use pandas to convert array to dataframe
df=pd.DataFrame(Data, columns=header)

print(df)

plt.plot(df['Systeemtijd'], df['Waarde'])
plt.show();




