using MoreLinq;
using NeverBounce.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NeverBounce
{
    public class NeverBounce
    {
        private NeverBounceSdk sdk;

        public NeverBounce(string apiKey)
        {
            sdk = new NeverBounceSdk(apiKey);
        }

        public IEnumerable<NeverBounceValidationResult> GetNeverBounceValidationResult(IEnumerable<string> recipients)
        {
            if (recipients.Count() == 0)
            {
                return Enumerable.Empty<NeverBounceValidationResult>();
            }

            var jobIds = new List<int>();

            foreach (var batch in recipients.Batch(1000))
            {
                var model = new JobCreateSuppliedDataRequestModel();
                model.auto_parse = true;
                model.auto_start = true;
                model.input = new List<object>();
                foreach (var r in batch)
                {
                    model.input.Add(new { email = r });
                }

                // Create job from supplied data
                JobCreateResponseModel resp = sdk.Jobs.CreateFromSuppliedData(model).Result;
                jobIds.Add(resp.job_id);
            }

            var result = new List<NeverBounceValidationResult>();
            foreach (var jobId in jobIds)
            {
                var emails = GetEmailJobValidationResult(jobId);
                result.AddRange(emails);
            }

            return result;
        }

        private List<NeverBounceValidationResult> GetEmailJobValidationResult(int jobId)
        {
            // Create job status model
            var model = new JobStatusRequestModel();
            model.job_id = jobId;

            // Query job status
            JobStatusResponseModel resp = sdk.Jobs.Status(model).Result;

            while (!resp.job_status.Equals("complete", StringComparison.InvariantCultureIgnoreCase))
            {
                Task.Delay(new TimeSpan(0, 0, 10)).Wait();
                resp = sdk.Jobs.Status(model).Result;
            }

            // Create job download model
            var downloadModel = new JobDownloadRequestModel();
            downloadModel.job_id = jobId;

            // Download jobs' data
            var validatedResult = sdk.Jobs.Download(downloadModel).Result;

            var result = new List<NeverBounceValidationResult>();

            using (var reader = new StringReader(validatedResult))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var split = line.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                    var validationStatus = split[1];

                    var upperCased = validationStatus.First().ToString().ToUpper() + validationStatus.Substring(1);
                    var status = (NeverBounceValidationStatus)Enum.Parse(typeof(NeverBounceValidationStatus), upperCased);

                    result.Add(new NeverBounceValidationResult
                    {
                        Email = split[0].Trim('"'),
                        Status = status
                    });
                }
            }

            return result;
        }
    }
}