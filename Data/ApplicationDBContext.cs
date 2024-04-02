using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CLI_DBMS.Models;

namespace CLI_DBMS.Data
{
    internal class ApplicationDBContext : DbContext
    {
        public virtual DbSet<Book> Books { get; set; }
        public virtual DbSet<Author> Authors { get; set; }
        public virtual DbSet<BookAuthor> BookAuthors { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            string connectionString = @"Data Source=localhost\SQLEXPRESS;database=BookDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";
            optionsBuilder.UseSqlServer(connectionString);
        }

        // entity confiiguration
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // "fluent API"

            // primary key of book
            modelBuilder.Entity<Book>()
                .HasKey(book => book.Id);
            modelBuilder.Entity<Book>()
                .Property(book => book.Id).ValueGeneratedNever();

            // primary key of author
            modelBuilder.Entity<Author>()
                .HasKey(author => author.Id);
            modelBuilder.Entity<Author>()
                .Property(author => author.Id).ValueGeneratedNever();

            // foreign keys of book author
            modelBuilder.Entity<BookAuthor>()
                .HasOne(bookAuthor => bookAuthor.Book)
                .WithMany(book => book.BookAuthors)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookAuthor>()
                .HasOne(bookAuthor => bookAuthor.Author)
                .WithMany(author => author.BookAuthors)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // primary key of bookauthor
            modelBuilder.Entity<BookAuthor>()
                .HasKey(bookAuthor => new { bookAuthor.AuthorId, bookAuthor.BookId });
        }
    }
}