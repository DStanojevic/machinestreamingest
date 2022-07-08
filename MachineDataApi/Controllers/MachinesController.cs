using MachineDataApi.Implementation.Services;
using MachineDataApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MachineDataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MachinesController : ControllerBase
    {
        private readonly IMachineDataService _machineDataService;

        public MachinesController(IMachineDataService machineDataService)
        {
            _machineDataService = machineDataService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var machineIds = await _machineDataService.GetMachines();
            return Ok(machineIds);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute]Guid id, [FromQuery] PagingParams? pagingParams)
        {
            if (pagingParams == null)
                pagingParams = new PagingParams();

            var machineDataResult = await _machineDataService.GetMachineDataPaged(id, pagingParams.Skip, pagingParams.Take);
            return machineDataResult.Match(
                some: p => (IActionResult)Ok(p),
                none: () => NotFound());
        }
    }
}
