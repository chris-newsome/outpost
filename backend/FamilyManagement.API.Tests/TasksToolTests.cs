using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FamilyManagement.API.Application.Assistant.Tools;
using FamilyManagement.API.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class TasksToolTests
{
    [Fact]
    public async Task CreatesAndListsTasks()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "tasks-tool")
            .Options;
        using var db = new AppDbContext(options);
        var tool = new TasksTool(db);
        var fid = Guid.NewGuid();
        var args = new JsonObject { ["action"] = "create_task", ["title"] = "Buy milk" };
        var res = await tool.InvokeAsync(fid, args, default);
        Assert.Equal("tasks", res.Name);

        var list = await tool.InvokeAsync(fid, new JsonObject { ["action"] = "list_tasks" }, default);
        var json = System.Text.Json.JsonSerializer.Serialize(list.Result);
        Assert.Contains("Buy milk", json);
    }
}
