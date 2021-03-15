using Oxide.Core;
using Oxide.Core.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("SQL", "TeamDMA", "0.0.1")]
    [Description("Manages MySQL Database")]
    class SQL : RustPlugin
    {
        Core.MySql.Libraries.MySql sqlLibrary = Interface.Oxide.GetLibrary<Core.MySql.Libraries.MySql>();
        Connection sqlConnection = null;
        Sql sqlCommand = null;

        string hostname = "45.89.126.3";
        int port = 3306;
        string dbname = "s3998_rust";
        string username = "u3998_Ko5bGz5pUY";
        string password = "gU=+.xbmYLcViFkM0kK^n1vW";

        float timer1 = 2f; // in s
        float timer2 = 120f; // in s
        float waitUntilDelete = 600f; // in s

        string addressToPing = "google.com";
        
        string sqlQuery = "INSERT INTO serverfps (data) VALUES(@0)";
        string sqlQueryDeleteOldEntries = "DELETE FROM serverfps WHERE cur_timestamp < NOW() - INTERVAL 7 DAY";
        string sqlQueryDeleteUnnecessaryEntries = "DELETE FROM serverfps WHERE (TIME(cur_timestamp) BETWEEN '00:59:30' AND '01:10:00') OR (TIME(cur_timestamp) BETWEEN '11:59:30' AND '12:10:00')";

        string sqlQueryPlayers = "INSERT INTO serverplayers (data) VALUES(@0)";
        string sqlQueryDeleteOldEntriesPlayers = "DELETE FROM serverplayers WHERE cur_timestamp < NOW() - INTERVAL 30 DAY";
        string sqlQueryDeleteUnnecessaryEntriesPlayers = "DELETE FROM serverplayers WHERE TIME(cur_timestamp) BETWEEN '00:59:30' AND '01:10:00' OR (TIME(cur_timestamp) BETWEEN '11:59:30' AND '12:10:00')";

        private void Init()
        {
            sqlConnection = sqlLibrary.OpenDb(hostname, port, dbname, username, password, this);

            timer.Once(waitUntilDelete, () =>
            {
                DeleteEntries(sqlCommand);
            });
            
            timer.Every(timer1, () =>
            {
                int serverfps = GetFPS();
                // fps
                sqlCommand = Oxide.Core.Database.Sql.Builder.Append(sqlQuery, serverfps);
                SendQuery(sqlCommand, sqlConnection);
            });

            //Playerstats
            timer.Every(timer2, () =>
            {
                int players = GetPlayersCount();
                // players
                sqlCommand = Oxide.Core.Database.Sql.Builder.Append(sqlQueryPlayers, players);
                SendQuery(sqlCommand, sqlConnection);
            });
        }
        private void Unload()
        {
            if(sqlConnection != null)
            {
                DeleteEntries(sqlCommand);

                sqlLibrary.CloseDb(sqlConnection);
            }
        }
        private void SendQuery(Sql command, Connection con)
        {
            if(sqlConnection != null)
            {
                sqlLibrary.ExecuteNonQuery(command, con);
            }
        }
        private void DeleteEntries(Sql command)
        {
            Puts("Deleting old and unnecessary entries...");

            // delete old entries
            command = Oxide.Core.Database.Sql.Builder.Append(sqlQueryDeleteOldEntries);
            sqlLibrary.ExecuteNonQuery(command, sqlConnection);
            // delete between 00:59:30 and 01:10:00
            command = Oxide.Core.Database.Sql.Builder.Append(sqlQueryDeleteUnnecessaryEntries);
            sqlLibrary.ExecuteNonQuery(command, sqlConnection);

            // same with pings, delete old entries
            command = Oxide.Core.Database.Sql.Builder.Append(sqlQueryDeleteOldEntriesPlayers);
            sqlLibrary.ExecuteNonQuery(command, sqlConnection);
            // delete between 00:59:30 and 01:10:00
            command = Oxide.Core.Database.Sql.Builder.Append(sqlQueryDeleteUnnecessaryEntriesPlayers);
            sqlLibrary.ExecuteNonQuery(command, sqlConnection);

            Puts("Deleted.");
        }
        private int GetFPS()
        {
            return Performance.current.frameRate;
        }
        private int GetPlayersCount()
        {
            return BasePlayer.activePlayerList.Count;
        }
    }
}
