using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI_DBMS.Models
{
    internal class BookAuthor
    {
        // self properties

        // fk properties
        public int BookId { get; set; }

        public int AuthorId { get; set; }

        // navigation properties

        public virtual Book Book { get; set; }
        public virtual Author Author { get; set; }
    }
}