namespace API.Helpers
{
    public class UserParams : PaginationParams
    {
        public string CurrentUsername { get; set; }
        public string Gender { get; set; }
        public int MinimumAge { get; set; }
        public int MaximumAge { get; set; }

        public string OrderBy { get; set; } = "lastActive";

    }
}
