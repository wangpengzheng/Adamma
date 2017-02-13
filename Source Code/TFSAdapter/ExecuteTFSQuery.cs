using System;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Data;

namespace TFSAdapter
{
    public class ExecuteTFSQuery
    {
        private string serverPath;

        public ExecuteTFSQuery()
        { }

        public ExecuteTFSQuery(string _serverPath)
        {
            serverPath = _serverPath;
        }

        public DataTable QueryTeamFoundationServerViaWiq(String _QueryStr)
        {
            DataTable dtResult = new DataTable() { TableName = "QueryResult" };
            try
            {
                TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(serverPath));

                WorkItemStore workItemStore = (WorkItemStore)tpc.GetService(typeof(WorkItemStore));

                WorkItemCollection queryResults = workItemStore.Query(_QueryStr);

                dtResult = this.renderWorkItemAsDataTable(queryResults);
            }
            catch
            {
                return null;
            }

            return dtResult;
        }

        /// <summary>
        /// refer the link: http://forums.asp.net/t/1178002.aspx
        /// </summary>
        /// <param name="_wiCollection"></param>
        /// <returns></returns>
        public DataTable renderWorkItemAsDataTable(WorkItemCollection _wiCollection)
        {
            DataTable sourceDt = new DataTable();

            if (_wiCollection != null)
            {
                foreach (FieldDefinition fieldDef in _wiCollection.DisplayFields)
                {
                    sourceDt.Columns.Add(fieldDef.Name, fieldDef.SystemType);
                }

                foreach (WorkItem workItem in _wiCollection)
                {
                    DataRow dr = sourceDt.NewRow();
                    foreach (FieldDefinition fieldDef in _wiCollection.DisplayFields)
                    {
                        try
                        {
                            dr[fieldDef.Name] = workItem[fieldDef.Name];
                        }
                        catch
                        {
                            dr[fieldDef.Name] = DBNull.Value;
                        }
                    }

                    sourceDt.Rows.Add(dr);
                }
            }

            return sourceDt;
        }
    }
}
