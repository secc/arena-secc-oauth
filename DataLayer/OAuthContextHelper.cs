using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arena.Custom.SECC.OAuth.DataLayer
{
    public class OAuthContextHelper
    {
        public static OAuthDataContext GetContext()
        {
            Arena.DataLib.SqlDbConnection conn = new DataLib.SqlDbConnection();
            return new OAuthDataContext( conn.GetDbConnection() );
        }
    }
}
