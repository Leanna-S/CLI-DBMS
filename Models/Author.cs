using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLI_DBMS.Models
{
    internal class Author
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }

        public virtual ICollection<BookAuthor> BookAuthors { get; set; }
    }
}