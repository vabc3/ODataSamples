using System;
using System.Collections.Generic;
using Microsoft.OData.Core;
using ODataSamples.Common;
using ODataSamples.Common.Model;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace ODataSamples.Writer
{
    class Program
    {
        private static readonly Uri ServiceRoot = new Uri("http://demo/odata.svc/");
        private static readonly ParserExtModel ExtModel = new ParserExtModel();
        private static readonly ODataFeed Feed;
        private static readonly ODataComplexValue Address1;
        private static readonly ODataEntry PersonEntry;
        private static readonly ODataEntry PetEntry;
        private static readonly ODataEntry FishEntry;
        private static readonly ODataNavigationLink PersonFavouritePetNavigationLink;
        private static readonly ODataNavigationLink PersonPetsNavigationLink;
        private static readonly ODataMessageWriterSettings BaseSettings = new ODataMessageWriterSettings()
        {
            ODataUri = new ODataUri { ServiceRoot = ServiceRoot },
            DisableMessageStreamDisposal = true,
            Indent = true,
        };

        static Program()
        {
            #region Feed and entry definition
            Feed = new ODataFeed();

            Address1 = new ODataComplexValue()
            {
                InstanceAnnotations = new List<ODataInstanceAnnotation>()
                {
                    new ODataInstanceAnnotation("ns.ann2", new ODataPrimitiveValue("hi"))
                },
                TypeName = "TestNS.Address", // Need this for parsed model.
                Properties = new List<ODataProperty>
                {
                    new ODataProperty()
                    {
                        Name = "ZipCode",
                        Value = "200",
                    },
                },
            };

            #region PersonEntry
            PersonEntry = new ODataEntry()
            {
                InstanceAnnotations = new List<ODataInstanceAnnotation>()
                {
                    new ODataInstanceAnnotation("ns.ann1", new ODataPrimitiveValue("hi"))
                },
                Properties = new List<ODataProperty>
                {
                    new ODataProperty()
                    {
                        Name = "Id",
                        Value = 1,
                    },
                    new ODataProperty()
                    {
                        Name = "Name",
                        Value = "Shang",
                    },
                    new ODataProperty()
                    {
                        Name = "Addr",
                        Value = Address1
                    }
                },
            };
            #endregion

            #region PetEntry
            PetEntry = new ODataEntry()
            {
                Properties = new List<ODataProperty>
                {
                    new ODataProperty()
                    {
                        Name = "Id",
                        Value = 1,
                    },
                    new ODataProperty()
                    {
                        Name = "Color",
                        Value = new ODataEnumValue("Cyan")
                    },
                },
            };
            #endregion PetEntry

            FishEntry = new ODataEntry()
            {
                TypeName = "TestNS.Fish",
                Properties = new List<ODataProperty>
                {
                    new ODataProperty()
                    {
                        Name = "Id",
                        Value = 2,
                    },
                    new ODataProperty()
                    {
                        Name = "Color",
                        Value = new ODataEnumValue("Blue"),
                    },
                    new ODataProperty()
                    {
                        Name = "Name",
                        Value = "Qin",
                    },
                },
            };
            #endregion

            PersonFavouritePetNavigationLink = new ODataNavigationLink
            {
                Name = "FavouritePet",
                AssociationLinkUrl = new Uri("Person(1)/FavouritePetPet/$ref", UriKind.Relative),
                Url = new Uri("Person(1)/FavouritePetPet", UriKind.Relative),
                IsCollection = false
            };

            PersonPetsNavigationLink = new ODataNavigationLink
            {
                Name = "Pets",
                AssociationLinkUrl = new Uri("Person(1)/Pets/$ref", UriKind.Relative),
                Url = new Uri("Person(1)/Pets", UriKind.Relative),
                IsCollection = true
            };
        }

        static void Main(string[] args)
        {
            var st = new Stopwatch();
            st.Start();
                WriteFeedBench3();
            Console.WriteLine(st.Elapsed);
            //WriteTopLevelFeed();
            //WriteTopLevelEntry();
            //ContainmentTest.FeedWriteReadNormal();
            //WriteTopLevelEntityReferenceLinks();
            //WriteInnerEntityReferenceLink();
        }

        private static void WriteFeedBench1(bool enableFullValidation = true, int count = 400000)
        {
            Console.WriteLine("WriteFeedBench1, enableFullValidation:{0}", enableFullValidation);

            var msg = ODataSamplesUtil.CreateMessage();

            var settings = new ODataMessageWriterSettings(BaseSettings)
            {
                EnableFullValidation = enableFullValidation
            };

            using (var omw = new ODataMessageWriter((IODataResponseMessage)msg, settings, ExtModel.Model))
            {
                var writer = omw.CreateODataFeedWriter(ExtModel.PetSet);
                writer.WriteStart(Feed);
                for(var i = 0; i < count; i++) { 
                writer.WriteStart(PetEntry);
                writer.WriteEnd();
              
                }
                writer.WriteEnd();
            }

            //Console.WriteLine(ODataSamplesUtil.MessageToString(msg));
            Console.WriteLine(ODataSamplesUtil.MessageToString(msg).Length);
        }

        private static void WriteFeedBench2(bool enableFullValidation = true, int count = 400000)
        {
            Console.WriteLine("WriteFeedBench2, enableFullValidation:{0}", enableFullValidation);

            var msg = ODataSamplesUtil.CreateMessage();
            var s0 = (MemoryStream)msg.GetStream();
            var settings = new ODataMessageWriterSettings(BaseSettings)
            {
                EnableFullValidation = enableFullValidation
            };

            var lc = new object();

            using (var omw = new ODataMessageWriter((IODataResponseMessage)msg, settings, ExtModel.Model))
            {
                var writer = omw.CreateODataFeedWriter(ExtModel.PetSet);
                writer.WriteStart(Feed);
                Task[] ts = new Task[count];
                for (var i = 0; i < count; i++)
                {
                    var b = i;
                    ts[i] = Task.Run(() => {
                        var msg1 = ODataSamplesUtil.CreateMessage();
                        using (var omw1 = new ODataMessageWriter((IODataResponseMessage)msg1, settings, ExtModel.Model))
                        {
                            var writer1 = omw1.CreateODataEntryWriter(ExtModel.PetSet);
                            writer1.WriteStart(PetEntry);
                            writer1.WriteEnd();
                            //Console.WriteLine(b);
                            var s1 = msg1.GetStream();
                            s1.Seek(0, SeekOrigin.Begin);
                            lock (lc) { 
                            s1.CopyTo(s0);
                            }
                        }
                    });
                }
                Task.WaitAll(ts);
                writer.WriteEnd();
            }

            //Console.WriteLine(ODataSamplesUtil.MessageToString(msg));
            Console.WriteLine(ODataSamplesUtil.MessageToString(msg).Length);
        }

        private static void WriteFeedBench3(bool enableFullValidation = true, int count = 400000)
        {
            Console.WriteLine("WriteFeedBench3, enableFullValidation:{0}", enableFullValidation);

            var msg = ODataSamplesUtil.CreateMessage();
            var s0 = (MemoryStream)msg.GetStream();
            var settings = new ODataMessageWriterSettings(BaseSettings)
            {
                EnableFullValidation = enableFullValidation
            };

            using (var omw = new ODataMessageWriter((IODataResponseMessage)msg, settings, ExtModel.Model))
            {
                var writer = omw.CreateODataFeedWriter(ExtModel.PetSet);
                writer.WriteStart(Feed);
                Task[] ts = new Task[count];
                Stream[] mss = new Stream[count];
                int complete = 0;

                for (var i = 0; i < count; i++)
                {
                    var b = i;
                    ts[i] = Task.Run(() => {
                        var msg1 = ODataSamplesUtil.CreateMessage();
                        using (var omw1 = new ODataMessageWriter((IODataResponseMessage)msg1, settings, ExtModel.Model))
                        {
                            var writer1 = omw1.CreateODataEntryWriter(ExtModel.PetSet);
                            writer1.WriteStart(PetEntry);
                            writer1.WriteEnd();
                            //Console.WriteLine(b);
                            var s1 = msg1.GetStream();
                            s1.Seek(0, SeekOrigin.Begin);
                            mss[b] = s1;
                        }
                    });
                }
                Task.WaitAll(ts);
                for (var i = 0; i < count; i++)
                {
                    mss[i].CopyTo(s0);
                }

                writer.WriteEnd();
            }

            //Console.WriteLine(ODataSamplesUtil.MessageToString(msg));
            Console.WriteLine(ODataSamplesUtil.MessageToString(msg).Length);
        }

        private static void WriteTopLevelFeed(bool enableFullValidation = true)
        {
            Console.WriteLine("WriteTopLevelFeed, enableFullValidation:{0}", enableFullValidation);

            var msg = ODataSamplesUtil.CreateMessage();

            var settings = new ODataMessageWriterSettings(BaseSettings)
            {
                EnableFullValidation = enableFullValidation
            };

            using (var omw = new ODataMessageWriter((IODataResponseMessage)msg, settings, ExtModel.Model))
            {
                var writer = omw.CreateODataFeedWriter(ExtModel.PetSet);
                writer.WriteStart(Feed);
                writer.WriteStart(PetEntry);
                writer.WriteEnd();
                writer.WriteStart(FishEntry);
                writer.WriteEnd();
                writer.WriteEnd();
            }

            Console.WriteLine(ODataSamplesUtil.MessageToString(msg));
        }

        private static void WriteTopLevelEntry()
        {
            Console.WriteLine("WriteTopLevelEntry");

            var msg = ODataSamplesUtil.CreateMessage();
            msg.PreferenceAppliedHeader().AnnotationFilter = "*";

            using (var omw = new ODataMessageWriter((IODataResponseMessage)msg, BaseSettings, ExtModel.Model))
            {
                var writer = omw.CreateODataEntryWriter(ExtModel.People);
                writer.WriteStart(PersonEntry);

                writer.WriteStart(PersonPetsNavigationLink);
                writer.WriteStart(new ODataFeed());
                writer.WriteStart(PetEntry);
                writer.WriteEnd();
                writer.WriteEnd();
                writer.WriteEnd();

                writer.WriteStart(PersonFavouritePetNavigationLink);
                writer.WriteStart(PetEntry);
                writer.WriteEnd();
                writer.WriteEnd();

                writer.WriteEnd();
            }

            Console.WriteLine(ODataSamplesUtil.MessageToString(msg));
        }

        private static void WriteTopLevelEntityReferenceLinks()
        {
            Console.WriteLine("WriteTopLevelEntityReferenceLinks");

            var msg = ODataSamplesUtil.CreateMessage();
            msg.PreferenceAppliedHeader().AnnotationFilter = "*";

            var link1 = new ODataEntityReferenceLink() { Url = new Uri("http://demo/odata.svc/People(3)") };
            var link2 = new ODataEntityReferenceLink() { Url = new Uri("http://demo/odata.svc/People(4)") };

            var links = new ODataEntityReferenceLinks()
            {
                Links = new[] { link1, link2 }
            };

            using (var omw = new ODataMessageWriter((IODataResponseMessage)msg, BaseSettings, ExtModel.Model))
            {
                omw.WriteEntityReferenceLinks(links);
            }

            Console.WriteLine(ODataSamplesUtil.MessageToString(msg));
        }

        private static void WriteInnerEntityReferenceLink()
        {
            Console.WriteLine("WriteInnerEntityReferenceLink in Request Payload (odata.bind)");

            var msg = ODataSamplesUtil.CreateMessage();
            msg.PreferenceAppliedHeader().AnnotationFilter = "*";

            var link1 = new ODataEntityReferenceLink
            {
                Url = new Uri("http://demo/odata.svc/PetSet(Id=1,Color=TestNS.Color'Blue')")
            };
            var link2 = new ODataEntityReferenceLink
            {
                Url = new Uri("http://demo/odata.svc/PetSet(Id=2,Color=TestNS.Color'Blue')")
            };

            using (var omw = new ODataMessageWriter((IODataRequestMessage)msg, BaseSettings, ExtModel.Model))
            {
                var writer = omw.CreateODataEntryWriter(ExtModel.People);
                writer.WriteStart(PersonEntry);

                writer.WriteStart(PersonPetsNavigationLink);
                writer.WriteEntityReferenceLink(link1);
                writer.WriteEntityReferenceLink(link2);
                writer.WriteEnd();

                writer.WriteStart(PersonFavouritePetNavigationLink);
                writer.WriteEntityReferenceLink(link1);
                writer.WriteEnd();

                writer.WriteEnd();
            }

            Console.WriteLine(ODataSamplesUtil.MessageToString(msg));
        }
    }
}
