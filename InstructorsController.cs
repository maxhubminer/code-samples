using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NarodnyUniversity.Data;
using NarodnyUniversity.Models;
using NarodnyUniversity.Models.HelperViewModels;
using NarodnyUniversity.Helpers;

namespace NarodnyUniversity.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InstructorsController(ApplicationDbContext context)
        {
            _context = context;    
        }

        // GET: Instructors
        public async Task<IActionResult> Index(string sortOrder, string searchString, string currentFilter, int? page, int? personID, int? groupID)
        {
            return View(await ControllerHelper.GetViewModel("Instructors", _context, ViewData, sortOrder, searchString, currentFilter, page, personID, groupID) );            
        }

        // GET: Instructors/Details/5
        public async Task<IActionResult> Details(int? id, int? groupID)
        {
            if (id == null)
            {
                return NotFound();
            }

            // tk+ output groups where he's instructor, and where he's student
            var persons = await _context.Person
                                .Include(p => p.GroupsAsInstructor)
                                    .ThenInclude(p => p.Group)
                                .Include(p => p.GroupsAsStudent)
                                    .ThenInclude(p => p.Group)
                  .AsNoTracking()
                  .OrderBy(i => i.LastName)
                  .ToListAsync();

            var person = persons.SingleOrDefault(m => m.ID == id && m.IsInstructor);

            if (person == null)
            {
                return NotFound();
            }

            ViewData["PersonName"] = person.FullName;

            var viewModel = new PersonDetailsData();

            viewModel.Person = person;
            viewModel.GroupsAsInstructor = person.GroupsAsInstructor.Select(s => s.Group);
            viewModel.GroupsAsStudent = person.GroupsAsStudent.Select(s => s.Group);

            if (groupID != null)
            {
                ViewData["GroupID"] = groupID.Value;
                ViewData["GroupName"] = _context.Group.
                                            Where(x => x.ID == groupID).
                                            Single().Description;

                if (viewModel.GroupsAsInstructor.Any())
                {
                    var _group = viewModel.GroupsAsInstructor.
                                            Where(x => x.ID == groupID).
                                            SingleOrDefault();
                    if(_group != null)
                    {
                        viewModel.GroupAssignmentsAsStudent = _group.AssignmentsAsStudent;
                    }
                    
                }
                
                if (viewModel.GroupsAsStudent.Any() && null == viewModel.GroupAssignmentsAsStudent)
                {
                    var _group = viewModel.GroupsAsStudent.
                                            Where(x => x.ID == groupID).
                                            SingleOrDefault();

                    if (_group != null)
                    {
                        viewModel.GroupAssignmentsAsStudent = _group.AssignmentsAsStudent;
                    }
                }
            }

            return View(viewModel);

            /*
            var person = await _context.Person.Where(p => p.IsInstructor).SingleOrDefaultAsync(m => m.ID == id);
            if (person == null)
            {
                return NotFound();
            }

            return View(person);
            */
        }

        // GET: Instructors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Instructors/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,MiddleName,Username,IsStudent")] Person person)
        {
            person.IsActive = true;
            person.IsInstructor = true;

            if (ModelState.IsValid)
            {
                _context.Add(person);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(person);
        }

        // GET: Instructors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.Person.Where(p => p.IsInstructor).SingleOrDefaultAsync(m => m.ID == id);
            if (person == null)
            {
                return NotFound();
            }
            return View(person);
        }

        // POST: Instructors/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,FirstName,IsActive,IsInstructor,IsStudent,LastName,MiddleName,Username")] Person person)
        {
            if (id != person.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(person);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonExists(person.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            return View(person);
        }

        // GET: Instructors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.Person.Where(p => p.IsInstructor).SingleOrDefaultAsync(m => m.ID == id);
            if (person == null)
            {
                return NotFound();
            }

            return View(person);
        }

        // POST: Instructors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var person = await _context.Person.SingleOrDefaultAsync(m => m.ID == id);
            _context.Person.Remove(person);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool PersonExists(int id)
        {
            return _context.Person.Any(e => e.ID == id);
        }
    }
}
