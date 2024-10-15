
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var emulatorConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

var services = new ServiceCollection();

services.AddDbContextFactory<AppDataContext>(options =>
{
    options.UseCosmos(emulatorConnectionString, "TestDb");
});

var dbContextFactory = services.BuildServiceProvider().GetRequiredService<IDbContextFactory<AppDataContext>>();
var dbContext = dbContextFactory.CreateDbContext();
await dbContext.Database.EnsureCreatedAsync();

var member = new Member
{
    Id = Guid.NewGuid().ToString(),
    MemberType = MemberType.Admin,
    Name = "John Doe"
};

// This is working as expected. The conversion from enum to string is working
dbContext.Members.Add(member);
await dbContext.SaveChangesAsync();

dbContext = dbContextFactory.CreateDbContext();
// this is working as expected. The conversion from string to enum is working
var allMembers = await dbContext.Members.ToListAsync();

dbContext = dbContextFactory.CreateDbContext();
// This is not working. The conversion from enum to string is not working
// EXCEPTION: 'Invalid cast from 'System.Int32' to 'MemberType'
var adminMembers = await dbContext.Members.Where(p => p.MemberType == MemberType.Admin).ToListAsync();



public sealed class AppDataContext : DbContext
{
    public AppDataContext(DbContextOptions<AppDataContext> options) : base(options) { }

    public DbSet<Member> Members { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var memberModel = modelBuilder.Entity<Member>();
        memberModel.Property(p => p.Id).ToJsonProperty("id");
        memberModel.Property(p => p.MemberType).HasConversion(v => v.ToString(), v => Enum.Parse<MemberType>(v, true));
        memberModel.ToContainer("Members").HasNoDiscriminator().HasKey(p => p.Id);
        memberModel.HasPartitionKey(d => d.MemberType);
    }
}

public enum MemberType
{
    User,
    Admin
}

public record Member
{
    public string Id { get; set; }
    public MemberType MemberType { get; set; }
    public string Name { get; set; }
}