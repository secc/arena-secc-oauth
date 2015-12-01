using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Core;

using Arena.Custom.SECC.OAuth.DataLayer;
namespace Arena.Custom.SECC.OAuth
{
    public class PersonMerges
    {
        public static List<int> GetOldPersonIds( int personId )
        {
            using (OAuthDataContext context = OAuthContextHelper.GetContext())
            {
                return context.PersonMergedDatas.Where( p => p.new_person_id == personId )
                        .Select( p => p.old_person_id ).ToList();
            }
        }

    }
}
