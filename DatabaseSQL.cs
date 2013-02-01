using System;
using System.Collections.Generic;
using System.Text;

namespace pidgeon_sv
{
    public class DatabaseSQL : DB
    {
        public string Server = null;
        public string User = null;
        public bool Connected = false;

        public DatabaseSQL(Account _client)
        {
            this.client = _client;
        }
    }
}
