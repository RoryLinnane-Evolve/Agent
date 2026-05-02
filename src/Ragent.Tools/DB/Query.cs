using Ragent.Reflection;

namespace Ragent.Tools.DB;

[ToolCollection]
public class Query {
    [Tool(Id = "query_db", Name = "Query the database", Description = "Returns data.")]
    public static string Logic([ToolParam(Description = "The SQL query for the DB")] string query) {
        //to be thought out further.
        return "unable to execute query";
    }
}
