using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace ImportData
{

    public class DataBaseConfig
    {
        private static readonly object objLock = new object();
        private static DataBaseConfig instance = null;

        private IConfigurationRoot Config { get; }

        private DataBaseConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Config = builder.Build();
        }

        public static DataBaseConfig GetInstance()
        {
            if (instance == null)
            {
                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new DataBaseConfig();
                    }
                }
            }

            return instance;
        }

        public static string GetConfig(string name)
        {
            return GetInstance().Config.GetSection(name).Value;
        }
    }

    public interface ISQLHelp
    {
        int ExecuteNonQuery(string sql);
        List<int> ExecuteNonQuery(List<QueryCmd> cmds);

        int ExcuteNonQuery(string cmd, DynamicParameters param, bool flag = true);

        object ExecuteScalar(string cmd, DynamicParameters param, bool flag = true);

        T FindOne<T>(string cmd, DynamicParameters param, bool flag = true) where T : class, new();

        List<T> FindToList<T>(string sql) where T : class, new();

        int Execute(string sql, object param = null);
    }

    public class DapperMySQLHelp: ISQLHelp
    {
        private string _connectionString;
        public DapperMySQLHelp(string connectionString)
        {
            this._connectionString = connectionString;
        }

        protected DbConnection CreateConnection()
        {
            DbConnection conn = null;
            //暂不支持oracle
            switch (EDoc2Utility.DatabaseType)
            {
                case 1:
                    conn = new SqlConnection(this._connectionString);
                    break;
                default:
                    conn = new MySqlConnection(this._connectionString);
                    break;
            }
            conn.Open();
            return conn;
        }
        protected void Dispose(DbConnection conn)
        {
            if (conn.State != System.Data.ConnectionState.Closed)
            {
                conn.Close();
            }
            conn.Dispose();
        }

        #region +ExcuteNonQuery 增、删、改同步操作

        /// <summary>
        /// 增、删、改同步操作
        ///  </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="connection">链接字符串</param>
        /// <param name="cmd">sql语句</param>
        /// <param name="param">参数</param>
        /// <param name="flag">true存储过程，false sql语句</param>
        /// <returns>int</returns>
        public int ExcuteNonQuery(string cmd, DynamicParameters param, bool flag = true)
        {
            int result = 0;
            using (DbConnection con = CreateConnection())
            {
                if (flag)
                {
                    result = con.Execute(cmd, param, null, null, CommandType.StoredProcedure);
                }
                else
                {
                    result = con.Execute(cmd, param, null, null, CommandType.Text);
                }
                Dispose(con);
            }
            return result;
        }
        public int ExecuteNonQuery(string sql)
        {
            int result = 0;
            using (DbConnection con = CreateConnection())
            {
                DbCommand command = con.CreateCommand();
                command.CommandText = sql;
                result = command.ExecuteNonQuery();
                Dispose(con);
            }
            return result;
        }

        public List<int> ExecuteNonQuery(List<QueryCmd> queryCmd)
        {
            List<int> resultList = new List<int>();
            int tmp = 0;

            using (DbConnection con = CreateConnection())
            {
                foreach (var kvSqlAndParams in queryCmd)
                {
                    tmp = con.Execute(kvSqlAndParams.Sql, kvSqlAndParams.DynamicParameters, null, null, CommandType.Text);
                    resultList.Add(tmp);
                }

                Dispose(con);
            }

            return resultList;
        }

        #endregion

        #region +ExecuteScalar 同步查询操作
        /// <summary>
        /// 同步查询操作
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="connection">连接字符串</param>
        /// <param name="cmd">sql语句</param>
        /// <param name="param">参数</param>
        /// <param name="flag">true存储过程，false sql语句</param>
        /// <returns>object</returns>
        public object ExecuteScalar(string cmd, DynamicParameters param, bool flag = true)
        {
            object result = null;
            using (DbConnection con = CreateConnection())
            {
                if (flag)
                {
                    result = con.ExecuteScalar(cmd, param, null, null, CommandType.StoredProcedure);
                }
                else
                {
                    result = con.ExecuteScalar(cmd, param, null, null, CommandType.Text);
                }
                Dispose(con);
            }
            return result;
        }
        #endregion

        #region +FindOne  同步查询一条数据
        /// <summary>
        /// 同步查询一条数据
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="connection">连接字符串</param>
        /// <param name="cmd">sql语句</param>
        /// <param name="param">参数</param>
        /// <param name="flag">true存储过程，false sql语句</param>
        /// <returns>t</returns>
        public T FindOne<T>(string cmd, DynamicParameters param, bool flag = true) where T : class, new()
        {
            IDataReader dataReader = null;
            using (DbConnection con = CreateConnection())
            {
                if (flag)
                {
                    dataReader = con.ExecuteReader(cmd, param, null, null, CommandType.StoredProcedure);
                }
                else
                {
                    dataReader = con.ExecuteReader(cmd, param, null, null, CommandType.Text);
                }
                if (dataReader == null || !dataReader.Read()) return null;
                Type type = typeof(T);
                T t = new T();
                foreach (var item in type.GetProperties())
                {
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        //属性名与查询出来的列名比较
                        if (item.Name.ToLower() != dataReader.GetName(i).ToLower()) continue;
                        var kvalue = dataReader[item.Name];
                        if (kvalue == DBNull.Value) continue;
                        item.SetValue(t, kvalue, null);
                        break;
                    }
                }
                Dispose(con);
                return t;
            }
        }
        #endregion

        #region +FindToList  同步查询数据集合

        /// <summary>
        /// 同步查询数据集合
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="connection">连接字符串</param>
        /// <param name="sql">sql语句</param>
        /// <returns>t</returns>
        public List<T> FindToList<T>(string sql) where T : class, new()
        {
            using (DbConnection db = CreateConnection())
            {
                var query = db.Query<T>(sql, commandTimeout: db.ConnectionTimeout);
                var list= query.ToList();
                Dispose(db);
                return list;
            }
        }

        public int Execute(string sql, object param = null)
        {
            using (DbConnection db = CreateConnection())
            {
                if (param != null)
                {
                    int reuslt = db.Execute(sql, param, commandTimeout: db.ConnectionTimeout);
                    Dispose(db);
                    return reuslt;
                }
                else
                {
                    int reuslt = db.Execute(sql, commandTimeout: db.ConnectionTimeout);
                    Dispose(db);
                    return reuslt;
                }
            }
        }


        #endregion

        #region +QueryData  同步查询数据集合
        /// <summary>
                /// 同步查询数据集合
                /// </summary>
                /// <param name="cmd">sql语句</param>
                /// <param name="param">参数</param>
                /// <param name="flag">true存储过程，false sql语句</param>
                /// <returns>t</returns>
        public List<Dictionary<String, object>> QueryData(string connString,string cmd, object param = null, bool flag = false)
        {
            string connection = connString;
            IDataReader dataReader = null;
            using (DbConnection con = CreateConnection())
            {
                if (flag)
                {
                    dataReader = con.ExecuteReader(cmd, param, null, null, CommandType.StoredProcedure);
                }
                else
                {
                    dataReader = con.ExecuteReader(cmd, param, null, null, CommandType.Text);
                }
                List<Dictionary<String, object>> list = new List<Dictionary<string, object>>();
                Dictionary<String, object> dic = null;
                string colName = "";
                while (dataReader.Read())
                {
                    dic = new Dictionary<string, object>();

                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        colName = dataReader.GetName(i);
                        dic.Add(colName, dataReader[colName]);
                    }


                    if (dic.Keys.Count > 0)
                    {
                        list.Add(dic);
                    }
                }
                Dispose(con);
                return list;
            }
        }
        #endregion




        #region +ExecuteScalar 同步查询操作
        /// <summary>
        /// 同步查询操作
        /// </summary>
        /// <param name="cmd">sql语句</param>
        /// <param name="param">参数</param>
        /// <param name="flag">true存储过程，false sql语句</param>
        /// <returns>object</returns>
        public object ExecuteScalar(string connString, string cmd, object param = null, bool flag = false)
        {
            string connection = connString;
            object result = null;
            using (DbConnection con = CreateConnection())
            {
                if (flag)
                {
                    result = con.ExecuteScalar(cmd, param, null, null, CommandType.StoredProcedure);
                }
                else
                {
                    result = con.ExecuteScalar(cmd, param, null, null, CommandType.Text);
                }
                Dispose(con);
            }
            return result;
        }
        #endregion




        #region +QueryPage 同步分页查询操作
        /// <summary>
        /// 同步分页查询操作
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="orderBy">排序字段</param>
        /// <param name="pageIndex">当前页码</param>
        /// <param name="pageSize">页面容量</param>
        /// <param name="count">总条数</param>
        /// <param name="param">参数</param>
        /// <param name="strWhere">条件</param>
        /// <returns>返回结果的数据集合</returns>
        public List<Dictionary<string, Object>> QueryPage(string conn,string sql, string orderBy, int pageIndex, int pageSize, out int count, object param = null, string strWhere = "")
        {
            count = 0;
            List<Dictionary<String, Object>> list = new List<Dictionary<string, object>>();


            if (sql.Contains("where"))
            {
                sql = sql + strWhere;
            }
            else
            {
                sql = sql + " where 1=1 " + strWhere;
            }


            string strSQL = "SELECT (@i:=@i+1) AS row_id,tab.* FROM (" + sql + ")  AS TAB,(SELECT @i:=0) AS it ORDER BY " + orderBy + " LIMIT " + (pageIndex - 1) + "," + pageSize;


            list = QueryData(conn,strSQL, param, false);


            string strCount = "SELECT count(*) FROM (" + sql + ") tcount";
            count = Convert.ToInt32(ExecuteScalar(conn,strCount));






            return list;
        } 
        #endregion
    }
}
