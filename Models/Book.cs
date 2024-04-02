using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI_DBMS.Models
{
    internal class Book
    {
        // self properties
        public int Id { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        // fk properties

        // navigation properties
        public virtual ICollection<BookAuthor> BookAuthors { get; set; }
    }
}