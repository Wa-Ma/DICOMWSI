using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DicomWSI.DAL
{
    public static class SqlHelperExt
    {
        public static int AddRange(this IDataParameterCollection coll, IDataParameter[] par)
        {
            int i = 0;
            foreach (var item in par)
            {
                coll.Add(item);
                i++;
            }
            return i;
        }
    }

    #region SqlHelper
    public class SqlHelper
    {
        private IDbConnection conn = null;
        private IDbCommand cmd = null;
        private IDataReader dr = null;
        private DbType type = DbType.SQLSERVER;

        #region 创建数据库连接
        /// <summary>
        /// 创建数据库连接
        /// </summary>
        public SqlHelper(string connectionString)
        {
            conn = DBFactory.CreateDbConnection(type, connectionString);
        }
        #endregion

        #region 判断并打开conn
        /// <summary>
        /// 判断并打开conn
        /// </summary>
        /// <returns></returns>
        public IDbConnection CreatConn()
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            return conn;
        }
        #endregion

        #region 执行查询sql语句
        /// <summary>
        /// 执行查询sql语句
        /// </summary>
        /// <param name="sql">查询sql语句</param>
        /// <returns>返回一个表</returns>
        public DataTable ExecuteReader(string sql)
        {
            DataTable dt = new DataTable();
            using (cmd = DBFactory.CreateDbCommand(sql, CreatConn()))
            {
                using (dr = cmd.ExecuteReader())
                {
                    dt.Load(dr);
                }
            }
            conn.Close();
            return dt;
        }
        #endregion

        #region 执行查询带参的sql语句
        /// <summary>
        /// 执行查询带参的sql语句
        /// </summary>
        /// <param name="sql">查询sql语句</param>
        /// <param name="par">sql语句中的参数</param>
        /// <returns>返回一个表</returns>
        public DataTable ExecuteReader(string sql, IDataParameter[] par)
        {
            DataTable dt = new DataTable();
            using (cmd = DBFactory.CreateDbCommand(sql, CreatConn()))
            {
                cmd.Parameters.AddRange(par);
                using (dr = cmd.ExecuteReader())
                {
                    dt.Load(dr);
                }
            }
            conn.Close();
            return dt;
        }
        public DataTable ExecuteReader(string sql, IDataParameter par)
        {
            DataTable dt = new DataTable();
            using (cmd = DBFactory.CreateDbCommand(sql, CreatConn()))
            {
                cmd.Parameters.Add(par);
                using (dr = cmd.ExecuteReader())
                {
                    dt.Load(dr);
                }
            }
            conn.Close();
            return dt;
        }
        #endregion

        #region 执行增，删，改sql语句
        /// <summary>
        /// 执行无参的增，删，改sql语句
        /// </summary>
        /// <param name="sql">增，删，改的sql语句</param>
        /// <param name="par">sql语句中的参数</param>
        /// <returns>返回所影响的行数</returns>
        public int ExecuteNonQuery(string sql)
        {
            int result = 0;
            using (cmd = DBFactory.CreateDbCommand(sql, CreatConn()))
            {
                result = cmd.ExecuteNonQuery();
            }
            conn.Close();
            return result;
        }
        #endregion

        #region 执行带参的增，删，改sql语句
        /// <summary>
        /// 执行带参的增，删，改sql语句
        /// </summary>
        /// <param name="sql">增，删，改的sql语句</param>
        /// <param name="par">sql语句中的参数</param>
        /// <returns>返回所影响的行数</returns>
        public int ExecuteNonQuery(string sql, IDbDataParameter[] par)
        {
            int result = 0;
            using (cmd = DBFactory.CreateDbCommand(sql, CreatConn()))
            {
                cmd.Parameters.AddRange(par);
                result = cmd.ExecuteNonQuery();
            }
            conn.Close();
            return result;
        }
        public int ExecuteNonQuery(string sql, IDbDataParameter par)
        {
            int result = 0;
            using (cmd = DBFactory.CreateDbCommand(sql, CreatConn()))
            {
                cmd.Parameters.Add(par);
                result = cmd.ExecuteNonQuery();
            }
            conn.Close();
            return result;
        }
        #endregion

        #region 事务
        /// <summary>
        /// 执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="SQLList">SQL语句的哈希表（key为sql语句，value是该语句的OleDbParameter[]）</param>
        public bool ExecuteTransaction(Hashtable SqlList)
        {
            CreatConn();
            using (IDbTransaction trans = conn.BeginTransaction())
            {
                IDbCommand cmd = DBFactory.CreateDbCommand(type);
                try
                {
                    //循环
                    foreach (DictionaryEntry myDE in SqlList)
                    {
                        string cmdText = myDE.Key.ToString();
                        IDbDataParameter[] cmdParms = (IDbDataParameter[])myDE.Value;
                        PrepareCommand(cmd, conn, trans, cmdText, cmdParms);
                        int val = cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                    }
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    return false;
                }
                finally
                {
                    conn.Close();
                }
            }
            return true;
        }

        private void PrepareCommand(IDbCommand cmd, IDbConnection conn, IDbTransaction trans, string cmdText, IDataParameter[] cmdParms)
        {
            CreatConn();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = CommandType.Text;//cmdType;
            if (cmdParms != null)
                cmd.Parameters.AddRange(cmdParms);
        }
        #endregion
    }
    #endregion
}
