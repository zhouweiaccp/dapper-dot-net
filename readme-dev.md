
## 常用功能
```csharp
//add https://stackoverflow.com/questions/5957774/performing-inserts-and-updates-with-dapper
using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["myDbConnection"].ConnectionString))
{
    string insertQuery = @"INSERT INTO [dbo].[Customer]([FirstName], [LastName], [State], [City], [IsActive], [CreatedOn]) VALUES (@FirstName, @LastName, @State, @City, @IsActive, @CreatedOn)";

    var result = db.Execute(insertQuery, new
    {
        customerModel.FirstName,
        customerModel.LastName,
        StateModel.State,
        CityModel.City,
        isActive,
        CreatedOn = DateTime.Now
    });
}

using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["myDbConnection"].ConnectionString))
{
    string insertQuery = @"INSERT INTO [dbo].[Customer]([FirstName], [LastName], [State], [City], [IsActive], [CreatedOn]) VALUES (@FirstName, @LastName, @State, @City, @IsActive, @CreatedOn)";

    var result = db.Execute(insertQuery, customerViewModel);
}

//SELECT
using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["myDbConnection"].ConnectionString))
{
    string selectQuery = @"SELECT * FROM [dbo].[Customer] WHERE FirstName = @FirstName";

    var result = db.Query(selectQuery, new
    {
        customerModel.FirstName
    });
}

//UPDATE
using (IDbConnection db = new SqlConnection(ConfigurationManager.ConnectionStrings["myDbConnection"].ConnectionString))
{
    string updateQuery = @"UPDATE [dbo].[Customer] SET IsActive = @IsActive WHERE FirstName = @FirstName AND LastName = @LastName";

    var result = db.Execute(updateQuery, new
    {
        isActive,
        customerModel.FirstName,
        customerModel.LastName
    });
}


//Dictionary
string query = "SELECT * FROM MyTableName WHERE Foo = @Foo AND Bar = @Bar";

Dictionary<string, object> dictionary = new Dictionary<string, object>();
dictionary.Add("@Foo", "foo");
dictionary.Add("@Bar", "bar");

var results = connection.Query<MyTableName>(query, new DynamicParameters(dictionary));

//ExpandoObject
ExpandoObject param = new ExpandoObject();

IDictionary<string, object> paramAsDict = param as IDictionary<string, object>;
paramAsDict.Add("foo", 42);
paramAsDict.Add("bar", "test");

MyRecord stuff = connection.Query<MyRecord>(query, param);


//https://github.com/ccrookston/Dapper.SimpleRepository
```