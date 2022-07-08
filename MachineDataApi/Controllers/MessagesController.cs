using MachineDataApi.Implementation.Services;
using MachineDataApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace MachineDataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IMachineDataService _machineDataService;

        public MessagesController(IMachineDataService machineDataService)
        {
            _machineDataService = machineDataService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery]PagingParams? pagingParams)
        {
            pagingParams ??= new PagingParams();

            var machineDataPagedResult = await _machineDataService.GetAllDataPaged(pagingParams.Skip, pagingParams.Take);
            return Ok(machineDataPagedResult);
        }

        // GET api/<MessagesController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var machineDataResult = await _machineDataService.GetMessage(id);
            return machineDataResult.Match(
                some: p => (IActionResult) Ok(p),
                none: () => NotFound());
        }
    }
}
