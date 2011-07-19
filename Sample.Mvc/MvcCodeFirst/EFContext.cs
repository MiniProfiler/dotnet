using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace SampleWeb.MvcCodeFirst
{
    public class EFContext : DbContext
    {
        public DbSet<Person> People { get; set; }
    }
}