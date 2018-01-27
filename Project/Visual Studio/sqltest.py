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






