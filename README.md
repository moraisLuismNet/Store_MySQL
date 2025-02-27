## Store_MySQL
ASP.NET Core Web API Store

![Store](img/1.png)
![Store](img/2.png)


## Program
``` 
var connectionString = builder.Configuration.GetConnectionString("Connection");

builder.Services.AddDbContext<StoreContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 31)));
);
``` 


## appsetting.Development.json
``` 
{
  "ConnectionStrings": {
    "Connection": "Server=localhost;Port=3306;Database=store;User=root;Password=root"
  }
}
``` 
