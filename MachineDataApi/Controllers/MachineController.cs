using MachineDataApi.Implementation.Services;
using MachineDataApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MachineDataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MachineController : ControllerBase
    {
        private readonly IMachineDataService _machineDataService;

        public MachineController(IMachineDataService machineDataService)
        {
            _machineDataService = machineDataService;
        }


        [HttpGet("{id}")]
        public Task<IActionResult> Get([FromRoute]Guid id, [FromQuery] PagingParams? pagingParams)
        {
            if (pagingParams == null)
                pagingParams = new PagingParams();

            return _machineDataService.GetMachineDataPaged(id, pagingParams.Skip, pagingParams.Take);
        }
    }
}
