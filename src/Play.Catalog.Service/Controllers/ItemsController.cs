using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Entities;
using Play.Catalog.Service.Extensions;
using Play.Common;

namespace Play.Catalog.Service.Controllers
{
    [ApiController]
    [Route("items")]
    [Authorize]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> _itemsRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public ItemsController(
            IRepository<Item> itemsRepository,
            IPublishEndpoint publishEndpoint
        )
        {
            _publishEndpoint = publishEndpoint;
            _itemsRepository = itemsRepository;

        }

        // Get
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            var items = (await _itemsRepository.GetAllAsync())
                .Select(item => item.AsDto());

            return Ok(items);
        }
        // Get by Id
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item == null) return NotFound();

            return item.AsDto();
        }
        // Post
        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            var item = new Item
            {
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.Now
            };

            await _itemsRepository.CreateAsync(item);

            await _publishEndpoint.Publish(new CatalogItemCreated(
                item.Id, item.Name, item.Description
            ));

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
        }
        // Put By Id
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await _itemsRepository.GetAsync(id);

            if (existingItem == null) return NotFound();

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await _itemsRepository.UpdateAsync(existingItem);

            await _publishEndpoint.Publish(new CatalogItemUpdated(
                existingItem.Id, existingItem.Name, existingItem.Description
            ));

            return NoContent();
        }
        // Delete By Id
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item == null) return NotFound();

            await _itemsRepository.RemoveAsync(item.Id);

            await _publishEndpoint.Publish(new CatalogItemDeleted(id));

            return NoContent();
        }
    }
}