using AgileManagement.Application;
using AgileManagement.Core;
using AgileManagement.Domain;
using AgileManagement.Mvc.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgileManagement.Mvc.Controllers
{

    [Authorize]
    public class ProjectController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProjectWithContributorsRequestService _projectWithContributorsRequestService;
        private readonly IMapper _mapper;

        public ProjectController(IProjectRepository projectRepository, IUserRepository userRepository,IProjectWithContributorsRequestService projectWithContributorsRequestService, IMapper mapper)
        {
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _projectWithContributorsRequestService = projectWithContributorsRequestService;
            _mapper = mapper;
        }

        public IActionResult Index()
        {
            
            return View();
        }

        /// <summary>
        /// İlgili Projede ilgili contributor'a erişim izni verir.
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="userId"></param>
        /// <param name="accepted"></param>
        /// <returns></returns>
        public IActionResult AcceptRequest(string projectId, string userId, bool accepted)
        {
            // Accepted Rejected Contributor Status
            // proje ile birlikte project contributor doldururuz ki projenin contributorlarına müdehale edelim
            var project =  _projectRepository.GetQuery()
                .Include(x=> x.Contributers)
                .FirstOrDefault(x=> x.Id == projectId);

            var user = _userRepository.Find(userId);

            if(user != null && project != null)
            {
                // aynı projede aynı contributor olamaz
               var contributor = project.Contributers.FirstOrDefault(x => x.UserId == user.Id);

                if (accepted)
                    contributor.ChangeProjectAccess(ContributorStatus.Accepted);
                else
                    contributor.ChangeProjectAccess(ContributorStatus.Rejected);
     
                _projectRepository.Save(); // project repo üzerinden contributor state değiştir.


            }

            return View();
        }

        public IActionResult List()
        {

            var response =  _projectWithContributorsRequestService.OnProcess();

            return View(response.Projects);
        }

        public IActionResult CreateProject()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateProject(ProjectCreateInputModel projectCreateInputModel)
        {
            if (ModelState.IsValid)
            {
                var project = new Project(name: projectCreateInputModel.Name, description: projectCreateInputModel.Description);

                _projectRepository.Add(project);
                _projectRepository.Save();

                ViewBag.Message = "Proje oluşturuldu";

                return View();
            }

            // Proje oluşturma sayfası
            return View();
        }


        [HttpGet]
        public IActionResult AddContributorRequest(string projectId)
        {
            var response = _projectWithContributorsRequestService.OnProcess(new ProjectWithContributorRequestDto { ProjectId = projectId });

            var projectContributorsId = response.Projects[0].Contributors.Select(x => x.UserId).ToList();

            // projeye tanımlanmış olan userların dropdownı
            ViewBag.UsersWithNoContributors = _userRepository.GetQuery().Where(x => projectContributorsId.Contains(x.Id) == false).Select(a => new SelectListItem
            {
                Text = a.Email,
                Value = a.Id.ToString()
            });
         

            return View(response.Projects[0]);

        }



        [HttpPost]
        public JsonResult AddContributorRequest([FromBody] ContributorInputModel model)
        {
            var project = _projectRepository.Find(model.ProjectId);

            foreach (var userId in model.UsersId)
            {
                project.AddContributor(new Contributor(userId));
            }

            _projectRepository.Save();


            return Json("OK");
           
        }

       
    }
}
