using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities
{
    //now table this attribute will override EF behavior and name the table as we specified below
    [Table("Photos")] 
    public class Photo
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public bool isMain { get; set; }
        public string PublicId { get; set; }
        

        //below these are the relationship properties (not required)
        //but they are used to till the EF that this is our fogien key and it can't be null
        //with out them the EF will be smart enough to determine the primary and forgien key
        // by it self, but it will be nullable and that might cause a business issues
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }

    }
}