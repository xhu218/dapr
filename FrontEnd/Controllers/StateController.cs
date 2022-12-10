using Dapr;
using Dapr.Client;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FrontEnd.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StateController : ControllerBase
    {
        private readonly ILogger<StateController> _logger;
        private readonly DaprClient _daprClient;

        const string STATE_STORE = "statestore-mysql";
        //const string STATE_STORE = "statestore";
        const string KEY_NAME = "guid";


        public StateController(ILogger<StateController> logger, DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        // 获取一个值
        [HttpGet]
        public async Task<ActionResult> GetAsync()
        {
            var result = await _daprClient.GetStateAsync<string>(STATE_STORE, KEY_NAME);
            return Ok(result);
        }

        // 获取一个值和etag
        [HttpGet("withetag")]
        public async Task<ActionResult> GetWithEtagAsync()
        {
            var (value,etag) = await _daprClient.GetStateAndETagAsync<string>(STATE_STORE, KEY_NAME);
            return Ok($"value is {value}, etag is {etag}");
        }

        //保存一个值
        [HttpPost]
        public async Task<ActionResult> PostAsync()
        {
            await _daprClient.SaveStateAsync<string>(STATE_STORE, KEY_NAME, Guid.NewGuid().ToString(), new StateOptions() { Consistency = ConsistencyMode.Strong });
            return Ok("done");
        }

        //删除一个值
        [HttpDelete]
        public async Task<ActionResult> DeleteAsync()
        {
            await _daprClient.DeleteStateAsync(STATE_STORE, KEY_NAME);
            return Ok("done");
        }

        //通过tag防止并发冲突，保存一个值
        [HttpPost("withtag")]
        public async Task<ActionResult> PostWithTagAsync()
        {
            var (value, etag) = await _daprClient.GetStateAndETagAsync<string>(STATE_STORE, KEY_NAME);
            await _daprClient.TrySaveStateAsync<string>(STATE_STORE, KEY_NAME, Guid.NewGuid().ToString(), etag);
            return Ok("done");
        }

        //通过tag防止并发冲突，删除一个值
        [HttpDelete("withtag")]
        public async Task<ActionResult> DeleteWithTagAsync()
        {
            var (value, etag) = await _daprClient.GetStateAndETagAsync<string>(STATE_STORE, KEY_NAME);
            return Ok(await _daprClient.TryDeleteStateAsync(STATE_STORE, KEY_NAME, etag));
        }


        // 从绑定获取一个值，健值name从路由模板获取
        [HttpGet("fromState/{name}")]
        public async Task<ActionResult> GetFromBindingAsync([FromState(STATE_STORE, "name")] StateEntry<string> state)
        {
            return Ok(state.Value);
        }


        // 根据绑定获取并修改值，健值name从路由模板获取
        [HttpPost("fromState/{name}")]
        public async Task<ActionResult> PostWithBindingAsync([FromState(STATE_STORE, "name")] StateEntry<string> state)
        {
            state.Value = Guid.NewGuid().ToString();
            return Ok(await state.TrySaveAsync());
        }


        // 获取多个个值
        [HttpGet("list")]
        public async Task<ActionResult> GetListAsync()
        {
            var result = await _daprClient.GetBulkStateAsync(STATE_STORE, new List<string> { KEY_NAME }, 10);
            return Ok(result);
        }

        // 删除多个个值
        [HttpDelete("list")]
        public async Task<ActionResult> DeleteListAsync()
        {
            var data = await _daprClient.GetBulkStateAsync(STATE_STORE, new List<string> { KEY_NAME }, 10);
            var removeList = new List<BulkDeleteStateItem>();
            foreach (var item in data)
            {
                removeList.Add(new BulkDeleteStateItem(item.Key, item.ETag));
            }
            await _daprClient.DeleteBulkStateAsync(STATE_STORE, removeList);
            return Ok("done");
        }
    }
}
