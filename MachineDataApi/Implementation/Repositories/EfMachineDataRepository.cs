﻿using MachineDataApi.Db;
using MachineDataApi.Models;
using Microsoft.EntityFrameworkCore;
using Optional;
using Optional.Async.Extensions;

namespace MachineDataApi.Implementation.Repositories
{
    public class EfMachineDataRepository : IMachineDataRepository
    {
        private readonly MachineDataDbContext _dbContext;

        public EfMachineDataRepository(MachineDataDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResult<MachineData>> GetAllDataPaged(int skip = 0, int take = 10)
        {
            var pagedData = await _dbContext.MachineData.OrderByDescending(x => x.TimeStamp).Skip(skip).Take(take).ToArrayAsync();
            return new PagedResult<MachineData>
            {
                Items = pagedData,
                TotalCount = await _dbContext.MachineData.CountAsync()
            };
        }

        public async Task<Option<MachineData>> GetItemById(Guid id) => (await _dbContext.MachineData.FirstOrDefaultAsync(x => x.Id == id)).SomeNotNull();

        public async Task<Option<PagedResult<MachineData>>> GetMachineDataPaged(Guid machineId, int skip = 0, int take = 10)
        {
            var machineOption = (await _dbContext.Machines.Include(p => p.Data.OrderByDescending(q => q.TimeStamp).Skip(skip).Take(take)).FirstOrDefaultAsync(x => x.Id == machineId)).SomeNotNull();
            return await machineOption.MapAsync(async x => new PagedResult<MachineData>
            {
                Items = x.Data.ToArray(),
                TotalCount = await _dbContext.MachineData.CountAsync(x => x.MachineId == machineId)
            });
        }

        public async Task<IEnumerable<Guid>> GetMachines()
        {
            return await _dbContext.Machines.Select(x => x.Id).ToArrayAsync();
        }

        public async Task Insert(MachineData machineData)
        {
            var machine = await _dbContext.Machines.FirstOrDefaultAsync(x => x.Id == machineData.Id);
            if(machine == null)
            {
                machine = new Machine
                {
                    Id = machineData.Id
                };
                _dbContext.Machines.Add(machine);
            }

            machine.Data.Add(machineData);

            await _dbContext.SaveChangesAsync();
        }
    }
}
