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
        public Task<IActionResult> Get([FromQuery]PagingParams? pagingParams)
        {
            if (pagingParams == null)
                pagingParams = new PagingParams();

            return _machineDataService.GetAllDataPaged(pagingParams.Skip, pagingParams.Take);
        }

        // GET api/<MessagesController>/5
        [HttpGet("{id}")]
        public Task<IActionResult> Get(Guid id)
        {
            return _machineDataService.GetMessage(id);
        }
    }
}
