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


        [HttpPost]
        public Task<IActionResult> RandomFail([FromBody]MachineData data)
        {
            var random = new Random();

            var det = random.Next(0, 100);
            if(det < 15)
            {
                throw new InvalidOperationException("Unknown error occured.");
            }
            if(det >= 15 && det < 30)
            {
                return Task.FromResult((IActionResult)BadRequest("Invalid request data"));
            }

            return Task.FromResult((IActionResult)Ok());
        }
    }
}
