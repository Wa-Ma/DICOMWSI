using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace DicomWSI.DAL
{
    public enum DbType
    {
        //Oracle,SqlServer,MySql,Access,SqlLite
        NONE,
        SQLSERVER,
        ACCESS
    }

    public class DBFactory
    {
        public static IDbConnection CreateDbConnection(DbType type, string connectionString)
        {
            IDbConnection conn = null;
            switch (type)
            {
                case DbType.SQLSERVER:
                    conn = new SqlConnection(connectionString);
                    break;
                case DbType.ACCESS:
                    conn = new OleDbConnection(connectionString);
                    break;
                case DbType.NONE:
                    throw new Exception("未设置数据库类型");
                default:
                    throw new Exception("不支持该数据库类型");
            }
            return conn;
        }


        public static IDbCommand CreateDbCommand(DbType type)
        {
            IDbCommand cmd = null;
            switch (type)
            {
                case DbType.SQLSERVER:
                    cmd = new SqlCommand();
                    break;
                case DbType.ACCESS:
                    cmd = new OleDbCommand();
                    break;
                case DbType.NONE:
                    throw new Exception("未设置数据库类型");
                default:
                    throw new Exception("不支持该数据库类型");
            }
            return cmd;
        }
        public static IDbCommand CreateDbCommand(string sql, IDbConnection conn)
        {
            DbType type = DbType.NONE;
            if (conn is SqlConnection)
                type = DbType.SQLSERVER;
            else if (conn is OleDbConnection)
                type = DbType.ACCESS;

            IDbCommand cmd = null;
            switch (type)
            {
                case DbType.SQLSERVER:
                    cmd = new SqlCommand(sql, (SqlConnection)conn);
                    break;
                case DbType.ACCESS:
                    cmd = new OleDbCommand(sql, (OleDbConnection)conn);
                    break;
                case DbType.NONE:
                    throw new Exception("未设置数据库类型");
                default:
                    throw new Exception("不支持该数据库类型");
            }
            return cmd;
        }
    }
}
