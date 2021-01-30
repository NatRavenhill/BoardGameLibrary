﻿using BoardGames.Models;
using BoardGames.Models.API;
using BoardGames.Models.Extensions;
using BoardGamesContextLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BoardGames.Controllers
{
    /// <summary>
    /// Controller for the Add Game page
    /// </summary>
    [Authorize]
    public class AddGameController : Controller
    {
        private IBoardGameContext db;

        public AddGameController(IBoardGameContext injectedContext)
        {
            db = injectedContext;
            Model = new AddGameViewModel();
        }

        public AddGameViewModel Model { get; }

        /// <summary>
        /// Action for the AddGame page
        /// </summary>
        /// <returns>The Add Game view</returns>
        public IActionResult AddGame(string searchText = "")
        {
            if (searchText == null)
                return View();

            Model.SearchText = searchText;

            Task<List<BoardGame>> task = GetData(searchText);
            task.Wait();

            Model.BoardGames = task.Result;
            return View(Model);
        }

        private async Task<List<BoardGame>> GetData(string searchText)
        {
            var boardGameAPI = BoardGameAPIHelper.BoardGameAPI;
            BoardGameList boardGameList = await boardGameAPI.SearchBoardGamesAsync(searchText);
            return boardGameList.BoardGames;

        }

        #region GameDetail

        private async Task<Item> GetGameDetail(int id)
        {
            var boardGameAPI = BoardGameAPIHelper.BoardGameAPI;
            ItemList resultList = await boardGameAPI.FindGameByIdAsync(id);
            Item foundGame = resultList.Items.FirstOrDefault();
            return foundGame;
        }

        /// <summary>
        /// Gets the GameDetail page for an object with the given id
        /// </summary>
        /// <param name="id">Id of game to retrieve</param>
        /// <returns>The game detail view</returns>
        public IActionResult GameDetail(int id, bool isAdded)
        {
            Task<Item> foundGame = GetGameDetail(id);

            var gameDetailVm = new GameDetailViewModel()
            {
                Item = foundGame.Result,
                Added = isAdded
            };

            return View(gameDetailVm);
        }

        #endregion GameDetail

        /// <summary>
        /// Adds the game to the database
        /// </summary>
        /// <param name="id">Id of game to retrieve</param>
        /// <returns>The game detail view</returns>
        public IActionResult AddToDatabase(int id)
        {
            Item foundGame = GetGameDetail(id).Result;
            if (foundGame == null)
                return NotFound();

            var gameDetail = foundGame.GetGameDetail();

            //set id to max + 1
            int maxID = 0;
            if (db.GameDetail.Any())
              maxID = db.GameDetail.Max(d => d.Id);
            gameDetail.Id = maxID + 1;

            var result = db.GameDetail.Add(gameDetail);
            if(result != null || db.GameDetail.Contains(gameDetail))
            {
                int entriesWritten = db.SaveChanges();
                if(entriesWritten > 0)
                {
                    Model.GameAddedText = $"{foundGame.Name.Value} was successfully added!";
                    return RedirectToAction("GameDetail", new { id = id, isAdded = true });

                }
            }
            
            Model.GameAddedText = $"{foundGame.Name} could not be added!";
            return StatusCode((int)HttpStatusCode.InternalServerError);


        }
    }
}