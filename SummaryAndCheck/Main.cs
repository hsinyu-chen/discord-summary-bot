using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SummaryAndCheck.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummaryAndCheck
{
    internal class Main(SummaryAndCheckDbContext dbContext, ILogger<Main> logger)
    {
        public async ValueTask RunAsync()
        {
            if(await dbContext.Database.EnsureCreatedAsync())
            {
                logger.LogInformation("database created");
            }
        }
    }
}
