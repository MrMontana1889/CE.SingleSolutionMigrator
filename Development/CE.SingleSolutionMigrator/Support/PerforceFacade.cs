// PerforceFacade.cs
// Copyright (c) Kris Culin. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perforce.P4;

namespace CE.SingleSolutionMigrator.Support
{
    public class PerforceFacade
    {
        #region Constructor
        public PerforceFacade(string server, string user, string pw, string workspace)
        {
            ServerName = server;
            UserName = user;
            Password = pw;
            Workspace = workspace;
        }
        #endregion

        #region Public Methods
        public ServerVersion Connect()
        {
            string uri = ServerName;
            string user = UserName;
            string client = Workspace;
            string pass = Password;

            var server = new Server(new ServerAddress(new Uri(uri).ToString()));
            var repo = new Repository(server);
            Connection = repo.Connection;

            Connection.UserName = user;
            Connection.Client = new Client();
            Connection.Client.Name = client;

            if (Connection.Connect(null))
            {
                Connection.Login(pass, null, null);

                var p4info = repo.GetServerMetaData(null);
                return p4info.Version;
            }

            return new ServerVersion();
        }
        public void Checkout(string filename)
        {
            int changeList = 0;
            FileSpec fileToCheckout = new FileSpec(null, null, new LocalPath(filename), null);
            EditCmdOptions options = new EditCmdOptions(EditFilesCmdFlags.None, changeList, null);

            try { Connection?.Client.EditFiles(options, fileToCheckout); }
            catch { }
        }
        public void Delete(string filename)
        {
            int changeList = 0;
            FileSpec fileToDelete = new FileSpec(null, null, new LocalPath(filename), null);
            DeleteFilesCmdOptions options = new DeleteFilesCmdOptions(DeleteFilesCmdFlags.None, changeList);

            Connection?.Client.DeleteFiles(options, fileToDelete);
        }
        public void RevertUnchanged()
        {
            RevertCmdOptions options = new RevertCmdOptions(RevertFilesCmdFlags.UnchangedOnly, 0);
            Connection?.Client.RevertFiles(options);
        }
        public void Dispose()
        {
            if (Connection != null)
            {
                Connection.Disconnect();
                Connection.Dispose();
            }
            Connection = null;
        }
        #endregion

        #region Public Properties

        #endregion

        #region Private Properties
        private Connection? Connection { get; set; }
        private string ServerName { get; }
        private string UserName { get; }
        private string Password { get; }
        private string Workspace { get; }
        #endregion
    }
}
