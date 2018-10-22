using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using DasBlog.Tests.FunctionalTests.Common;
using DasBlog.Tests.Support.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using newtelligence.DasBlog.Runtime;
using Xunit;

namespace DasBlog.Tests.FunctionalTests.ComponentTests
{
	public partial class BlogManagerTests : IClassFixture<ComponentTestPlatform>, IDisposable
	{
		[Fact]
		[Trait(Constants.CategoryTraitType, Constants.ComponentTestTraitValue)]
		public void Post_ForFirstInDay_CreatesDayEntryFile()
		{
			using (var sandbox = platform.CreateSandbox(Constants.VanillaEnvironment))
			{
				var blogManager = platform.CreateBlogManager(sandbox);
				var entry = MakeMiniimalEntry();
				blogManager.CreateEntry(entry);
				var testDataProcessor = platform.CreateTestDataProcessor(sandbox);
				var savedEntryId = testDataProcessor.GetBlogPostValue(DateTime.Today, entry.EntryId, "EntryId");
				Assert.Equal(entry.EntryId.ToString(), savedEntryId.value);
			}
		}

		[Fact]
		[Trait(Constants.CategoryTraitType, Constants.ComponentTestTraitValue)]
		public void Post_ForSEcondInDay_CreatesDayEntryFile()
		{
			using (var sandbox = platform.CreateSandbox(Constants.VanillaEnvironment))
			{
				var blogManager = platform.CreateBlogManager(sandbox);
				Entry firstEntry = MakeMiniimalEntry();
				blogManager.CreateEntry(firstEntry);
				Entry secondEntry = MakeMiniimalEntry();
				blogManager.CreateEntry(secondEntry);
				var testDataProcessor = platform.CreateTestDataProcessor(sandbox);
				var savedFirstEntryId = testDataProcessor.GetBlogPostValue(DateTime.Today, firstEntry.EntryId, "EntryId");
				Assert.Equal(firstEntry.EntryId.ToString(), savedFirstEntryId.value);
				var savedSecondEntryId = testDataProcessor.GetBlogPostValue(DateTime.Today, secondEntry.EntryId, "EntryId");
				Assert.Equal(secondEntry.EntryId.ToString(), savedSecondEntryId.value);
			}
		}
		[Fact]
		[Trait(Constants.CategoryTraitType, Constants.ComponentTestTraitValue)]
		public void Post_ForSEcondInDayWithoutCache_AddsEntryToDayEntryFile()
		{
			using (var sandbox = platform.CreateSandbox(Constants.VanillaEnvironment))
			{
				SaveEntryDirect(Path.Combine(sandbox.TestEnvironmentPath, Constants.ContentDirectory));
						// create the file and seed with an uncached post
				var blogManager = platform.CreateBlogManager(sandbox);
				var entry = MakeMiniimalEntry();
				blogManager.CreateEntry(entry);
				var testDataProcessor = platform.CreateTestDataProcessor(sandbox);
				var savedEntryId = testDataProcessor.GetBlogPostValue(DateTime.Today, entry.EntryId, "EntryId");
				Assert.Equal(entry.EntryId.ToString(), savedEntryId.value);
			}
		}
		[Fact]
		[Trait(Constants.CategoryTraitType, Constants.ComponentTestTraitValue)]
		public void Update_ExistngPost_ModifiesDayEntryFile()
		{
			using (var sandbox = platform.CreateSandbox(Constants.VanillaEnvironment))
			{
				var blogManager = platform.CreateBlogManager(sandbox);
				var testDataProcessor = platform.CreateTestDataProcessor(sandbox);
				Entry firstEntry = MakeMiniimalEntry();
				firstEntry.Title = "created";
				blogManager.CreateEntry(firstEntry);
				var savedFirstTitle = testDataProcessor.GetBlogPostValue(DateTime.Today, firstEntry.EntryId, "Title");
				Assert.Equal("created", savedFirstTitle.value);
				Entry secondEntry = MakeMiniimalEntry();
				secondEntry.EntryId = firstEntry.EntryId;
				secondEntry.Title = "updated";
				blogManager.UpdateEntry(secondEntry);
				var savedSecondTitle = testDataProcessor.GetBlogPostValue(DateTime.Today, secondEntry.EntryId, "Title");
				Assert.Equal("updated", savedSecondTitle.value);
			}
		}
		[Fact]
		[Trait(Constants.CategoryTraitType, Constants.ComponentTestTraitValue)]
		[Trait("Chosen", "2")]
		public void UpdateExistingPost_WithoutCache_ModifiesDayEntryFile()
		{
			using (var sandbox = platform.CreateSandbox(Constants.VanillaEnvironment))
			{
				var entryId = Guid.NewGuid().ToString();
				// create the file and seed with an uncached post
				SaveEntryDirect(Path.Combine(sandbox.TestEnvironmentPath, Constants.ContentDirectory)
				  ,entryId);
				//
				var blogManager = platform.CreateBlogManager(sandbox);
				var entry = MakeMiniimalEntry();
				entry.Title = "updated";
				entry.EntryId = entryId;
				blogManager.UpdateEntry(entry);
				var testDataProcessor = platform.CreateTestDataProcessor(sandbox);
				var savedTitle = testDataProcessor.GetBlogPostValue(DateTime.Today, entry.EntryId, "Title");
				Assert.Equal("updated", savedTitle.value);
			}
		}

		[Fact(Skip="true")]
		// TODO unskip: skipped as although it runs well in isolation it fails as port of a suite
		// turns out that if "DeletePost_WithoutCache_ModifiesDayEntryFile" runs first then
		// this test fails GetEntryForEdit() which presumably initialises the cache.
		// presumably there is no way in the app for an entry suddenly to appear.
		// the question is how to get the tests to run in isolation.
		[Trait(Constants.FailureTraitTraitType, Constants.FailsInSuiteTraitValue)]
		[Trait(Constants.DescriptionTraitType, "thows exception when run as part of suite")]
		[Trait(Constants.CategoryTraitType, Constants.ComponentTestTraitValue)]
		[Trait("Chosen", "1")]
		public void DeletePost_WithCache_ModifiesDayEntryFile()
		{
			using (var sandbox = platform.CreateSandbox(Constants.VanillaEnvironment))
			{
				var logger = platform.ServiceProvider.GetService<ILoggerFactory>().CreateLogger<BlogManagerTests>();
				logger.LogDebug($"test 1 started at {DateTime.Now} process: {System.Diagnostics.Process.GetCurrentProcess().Id}");
				var entryId = Guid.NewGuid().ToString();
				logger.LogDebug($"entryIId=={entryId}");
				// create the file and seed with an uncached post
				SaveEntryDirect(Path.Combine(sandbox.TestEnvironmentPath, Constants.ContentDirectory)
					, entryId);
				//
				logger.LogDebug($"before CreateBlogManager: dayentry file exists for {DateTime.Today} {DayEntryFileExists(Path.Combine(sandbox.TestEnvironmentPath, Constants.ContentDirectory), DateTime.Today)}");
				var blogManager = platform.CreateBlogManager(sandbox);
				logger.LogDebug($"after CreateBlogManager: dayentry file exists for {DateTime.Today} {DayEntryFileExists(Path.Combine(sandbox.TestEnvironmentPath, Constants.ContentDirectory), DateTime.Today)}");
				// make sure it is a valid entry which should cache it.
				Entry entry = blogManager.GetEntryForEdit(entryId);
				Assert.NotNull(entry);
				//
				blogManager.DeleteEntry(entryId);
				// make sure it has gone
				Assert.Throws<NullReferenceException>(() => blogManager.GetEntryForEdit(entryId));
				//
				var testDataProcessor = platform.CreateTestDataProcessor(sandbox);
/*
				var result = testDataProcessor.GetBlogPostValue(DateTime.Today, entry.EntryId, "Title");
				Assert.False(result.success); // the file should have been removed when the the one and only
				// post was deleted
*/

				Assert.Throws<System.IO.FileNotFoundException>(
					() => testDataProcessor.GetBlogPostValue(DateTime.Today, entry.EntryId, "Title"));
				logger.LogDebug($"test 1 finished at {DateTime.Now}");
			}
		}

		[Fact]
		[Trait(Constants.CategoryTraitType, Constants.ComponentTestTraitValue)]
		[Trait("Chosen", "1")]
		public void DeletePost_WithoutCache_ModifiesDayEntryFile()
		{
			Thread.Sleep(5000);
			var logger = platform.ServiceProvider.GetService<ILoggerFactory>().CreateLogger<BlogManagerTests>();
			logger.LogDebug($"test 2 started at {DateTime.Now} process: {System.Diagnostics.Process.GetCurrentProcess().Id}");
			using (var sandbox = platform.CreateSandbox(Constants.VanillaEnvironment))
			{
				var entryId = Guid.NewGuid().ToString();
				// create the file and seed with an uncached post
				SaveEntryDirect(Path.Combine(sandbox.TestEnvironmentPath, Constants.ContentDirectory)
					,entryId);
				//
				var blogManager = platform.CreateBlogManager(sandbox);
				blogManager.DeleteEntry(entryId);
				// make sure it has gone
				Assert.Throws<NullReferenceException>(() => blogManager.GetEntryForEdit(entryId));
				//
				var testDataProcessor = platform.CreateTestDataProcessor(sandbox);
				Assert.Throws<System.IO.FileNotFoundException >(
					() => testDataProcessor.GetBlogPostValue(DateTime.Today, entryId, "Title"));	
												// the file should have been removed when the the one and only
												// post was deleted
				logger.LogDebug($"test 2 ended at {DateTime.Now}");
			}
		}

		/// <summary>
		/// writes day entry file into the content directory 
		/// </summary>
		/// <param name="directory">e.g. c:/projects/dasblog-core.../Environments/Vanilla/content</param>
		private void SaveEntryDirect(string directory, string entryId = null)
		{
			var fileName = TestDataProcesor.GetBlogEntryFileName(DateTime.Today);
			var path = Path.Combine(directory, fileName);
			var str = string.Format(minimalBlogPostXml
			  , DateTime.Today.ToString("s", CultureInfo.InvariantCulture)
			  , DateTime.Now.ToString("s", CultureInfo.InvariantCulture)
			  , DateTime.Now.ToString("s", CultureInfo.InvariantCulture)
			  , entryId ?? Guid.NewGuid().ToString());
			File.WriteAllText(path, str);
		}

		private void DeleteDirectoryContentsDirect(string directory, string fileSpec = "*.*")
		{
			foreach (var file in Directory.EnumerateFiles(directory, fileSpec))
			{
				File.Delete(file);
			}
		}
		private bool DayEntryFileExists(string directory, DateTime dt)
		{
			var fileName = TestDataProcesor.GetBlogEntryFileName(dt);
			var path = Path.Combine(directory, fileName);
			return File.Exists(path);
		}
		private bool DayFeedbackFileExists(string directory, DateTime dt)
		{
			var fileName = TestDataProcesor.GetBlogFeedbackFileName(dt);
			var path = Path.Combine(directory, fileName);
			return File.Exists(path);
		}
		
		private static Entry MakeMiniimalEntry()
		{
			Entry entry = new Entry();
			entry.Initialize();
			entry.Title = string.Empty;
			entry.Content = string.Empty;
			entry.Description = string.Empty;
			entry.Categories = string.Empty;
			return entry;
		}


	}
}
