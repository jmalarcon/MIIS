using System;
using System.Linq;
using System.Collections.Generic;
using DotLiquid;
using MIISHandler;

namespace FilesEnumeratorParam
{
    public class Paginator : Drop
    {
        //Constructor. Takes an IEnumerable list of MIISFiles and sets all the properties
        public Paginator(IEnumerable<MIISFile> allFiles, int currentPage, int pageSize)
        {
            if (pageSize == 0) pageSize = 10;
            this.PerPage = pageSize < 0 ? Math.Abs(pageSize) : pageSize;
            //Number of files
            this.TotalFiles = allFiles.Count();
            //Number of pages
            this.TotalPages = (this.TotalFiles / pageSize) + ((this.TotalFiles % pageSize == 0) ? 0 : 1);
            //Current page
            currentPage = (currentPage <= 0) ? 1 : currentPage;
            this.Page = (currentPage > this.TotalPages) ? this.TotalPages : currentPage;

            //Reutn ronly the files that belong to the current pagination page
            this.Files = allFiles.Skip((this.Page - 1) * this.PerPage).Take(this.PerPage).ToList<MIISFile>();
        }

        //Files/Posts available for the current page
        //We have "files" and "posts" (to ease the use for Jekyll users). 
        //Both are the same but the "good" one is "files" and that's the recommended one
        public List<MIISFile> Files { get; private set; }
        public List<MIISFile> Posts { get { return Files; } }

        //Number of posts per page
        public int PerPage { get; private set; }
        
        //Total number of files/posts
        public int TotalFiles { get; private set; }
        public int TotalPosts { get { return TotalFiles; } }
        
        //Total number of pages in the pagination
        public int TotalPages { get; private set; }
        
        //The number of the current page
        public int Page { get; private set; }
        
        //The number of the previous page, or null if no previous page exists
        public int? PreviousPage
        {
            get
            {
                if (this.Page == 1)
                    return null;
                //else
                return this.Page - 1;
            }
        }
        //The number of the next page, or null if no previous page exists
        public int? NextPage
        {
            get
            {
                if (this.Page == this.TotalPages)
                    return null;
                //else
                return this.Page + 1;
            }
        }

    }
}
