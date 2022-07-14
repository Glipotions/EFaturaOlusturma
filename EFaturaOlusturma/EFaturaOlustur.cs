using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;

namespace EFaturaOlustur
{
    public class EFaturaOlustur
    {
        private void FaturaOlustur(SatisFaturaS fatura, List<UrunBilgileriL> faturaBilgileri)
        {
            var firmaParametre = AnaForm.FirmaParametreleri;
            string currencyId = "TRY", faturaNo = firmaParametre.FaturaNo + DateTime.Now.Year.ToString() + fatura.Kod;
            using var cariHesapBusiness = new CariHesapBusiness();
            var cariHesap = (CariHesap)cariHesapBusiness.Single(x => x.Id == txtFirmaUnvani.Id);


            InvoiceLineType[] FaturaHareketleri()
            {
                var lines = new List<InvoiceLineType>();
                //var source = urunBilgileriTable.Tablo.DataController.ListSource.Cast<UrunBilgileriL>();
                var lineNumber = 1;

                faturaBilgileri.ForEach(x =>
                {
                    var birimCode = x.Birim == Birim.Adet ? "C62" : x.Birim == Birim.Metre ? "MTR" : x.Birim == Birim.Gram ? "GRM" : x.Birim == Birim.Kg ? "KGM" : "CM";
                    var line = new InvoiceLineType
                    {
                        ID = new IDType { Value = lineNumber.ToString() },
                        //Note=new[] {new NoteType { Value=x.UrunAdi} } // Ürün Detay Açıklaması
                        InvoicedQuantity = new InvoicedQuantityType { unitCode = birimCode, Value = System.Convert.ToDecimal(x.Miktar) },
                        LineExtensionAmount = new LineExtensionAmountType { currencyID = currencyId, Value = (decimal)x.KdvHaricTutar },
                        AllowanceCharge = new[] {new AllowanceChargeType
                        {
                            ChargeIndicator = new ChargeIndicatorType { Value = false },
                            MultiplierFactorNumeric = new MultiplierFactorNumericType { Value = (decimal)x.IskontoOrani},
                            Amount=new AmountType2{currencyID = currencyId, Value=(decimal)x.IskontoTutari},     //ISKONTO EKLE ******
                            BaseAmount=new BaseAmountType{currencyID = currencyId, Value=(decimal)x.Tutar},
                        }},
                        TaxTotal = new TaxTotalType
                        {
                            TaxAmount = new TaxAmountType { currencyID = currencyId, Value = (decimal)x.KdvTutari },
                            TaxSubtotal = new[]
                            {
                                new TaxSubtotalType
                                {
                                    TaxableAmount=new TaxableAmountType{currencyID = currencyId, Value= x.NetTutar},
                                    TaxAmount=new TaxAmountType{currencyID= currencyId, Value=(decimal)x.KdvTutari},
                                    CalculationSequenceNumeric=new CalculationSequenceNumericType{Value=1},
                                    Percent=new PercentType1{Value=(decimal)x.KdvOrani},
                                    TaxCategory=new TaxCategoryType
                                    {
                                        TaxScheme=new TaxSchemeType{TaxTypeCode=new TaxTypeCodeType{Value="0015", name="KDV"}},
                                        Name=new NameType1{Value="KDV"}
                                    }
                                }
                            }
                        },
                        Item = new ItemType { Name = new NameType1 { Value = x.UrunKodu + " - " + x.UrunAdi } },
                        Price = new PriceType { PriceAmount = new PriceAmountType { currencyID = currencyId, Value = (decimal)x.BirimFiyat } }


                    };
                    lineNumber++;
                    lines.Add(line);
                });
                return lines.ToArray();
            }

            var invoice = new InvoiceType
            {
                UBLVersionID = new UBLVersionIDType { Value = "2.1" },
                CustomizationID = new CustomizationIDType { Value = "TR1.2" },
                ProfileID = new ProfileIDType { Value = "EARSIVFATURA" },
                ID = new IDType { Value = fatura.Kod }, // Kod Sayısının Yeterli olup olmamasını kontrol et
                CopyIndicator = new CopyIndicatorType { Value = false },
                UUID = new UUIDType { Value = Guid.NewGuid().ToString() },
                IssueDate = new IssueDateType { Value = fatura.Tarih },
                IssueTime = new IssueTimeType { Value = fatura.Tarih },
                InvoiceTypeCode = new InvoiceTypeCodeType { Value = "SATIS" },
                Note = new[] { new NoteType { Value = fatura.ToplamTutar.YaziIleTutar() },
                    new NoteType { Value = fatura.Aciklama } }, // DEĞİŞTİRİLECEK
                DocumentCurrencyCode = new DocumentCurrencyCodeType { Value = currencyId },
                //PaymentCurrencyCode=  (1/2 10. Video 2. dk)
                LineCountNumeric = new LineCountNumericType { Value = faturaBilgileri.Count },
                //InvoicePeriod  --> Periot varsa kullanılır
                AdditionalDocumentReference = new[] { new DocumentReferenceType
                {
                    ID = new IDType { Value = Guid.NewGuid().ToString() },
                    IssueDate = new IssueDateType { Value = fatura.Tarih },
                    DocumentType = new DocumentTypeType { Value = "XSLT" },
                    Attachment = new AttachmentType
                    {
                        EmbeddedDocumentBinaryObject = new EmbeddedDocumentBinaryObjectType
                        {
                            characterSetCode = "UTF-8",
                            encodingCode = "Base64",
                            filename = "EArchiveInvoice.xslt",
                            mimeCode = "application/xml",
                            Value = Encoding.UTF8.GetBytes(new StreamReader(new FileStream(Application.StartupPath + "\\" + "general.xslt", FileMode.Open, FileAccess.Read), Encoding.UTF8).ReadToEnd())
                        }
                    }
                }, new DocumentReferenceType
                {
                    ID = new IDType { Value = Guid.NewGuid().ToString() },
                    IssueDate = new IssueDateType { Value = fatura.Tarih },
                    DocumentTypeCode = new DocumentTypeCodeType { Value = "SendingType" },
                    DocumentType = new DocumentTypeType { Value = "ELEKTRONIK" },

                }
                },
                Signature = new[]
                {
                    new SignatureType
                    {
                        ID = new IDType { schemeID = "VKN_TCKN", Value = firmaParametre.VergiKimlikNo },       // Fİrma Parametreleri Vergi Numarası
                        SignatoryParty = new PartyType
                        {
                            PartyIdentification = new[] { new PartyIdentificationType { ID = new IDType { schemeID = "VKN", Value = firmaParametre.VergiNo } } },  // VERGİ KİMLİK NUMARASI
                            PostalAddress = new AddressType //(1/2) 12. Video 8. dk
                            {
                                Room = new RoomType { Value = firmaParametre.DaireNo }, // Firma Parametreleri
                                BuildingName = new BuildingNameType { Value = firmaParametre.BinaAdi },
                                BuildingNumber = new BuildingNumberType { Value = firmaParametre.BinaNo },
                                CitySubdivisionName = new CitySubdivisionNameType { Value = firmaParametre.Ilce },
                                CityName = new CityNameType { Value =firmaParametre.Sehir },
                                PostalZone = new PostalZoneType { Value = firmaParametre.PostaKodu },
                                Country = new CountryType { Name = new NameType1 { Value = firmaParametre.Ulke }, }
                            },
                        },
                        DigitalSignatureAttachment = new AttachmentType { ExternalReference = new ExternalReferenceType { URI = new URIType { Value = "#Signature_" + faturaNo } } }
                    },
                },
                AccountingSupplierParty = new SupplierPartyType
                {
                    Party = new PartyType
                    {
                        PartyIdentification = new[]
                        {
                            new PartyIdentificationType { ID = new IDType { schemeID = "VKN", Value = firmaParametre.VergiNo } }, // VERGİ KİMLİK NUMARASI
                            new PartyIdentificationType { ID = new IDType { schemeID = "MERSISNO", Value = firmaParametre.MersisNo } } // MERSİS NO
                        },
                        PartyName = new PartyNameType { Name = new NameType1 { Value = firmaParametre.KurumAdi } },
                        PostalAddress = new AddressType //(1/2) 12. Video 8. dk
                        {
                            Room = new RoomType { Value = firmaParametre.DaireNo }, // Firma Parametreleri
                            BuildingName = new BuildingNameType { Value = firmaParametre.BinaAdi },
                            BuildingNumber = new BuildingNumberType { Value = firmaParametre.BinaNo },
                            CitySubdivisionName = new CitySubdivisionNameType { Value = firmaParametre.Ilce },
                            CityName = new CityNameType { Value = firmaParametre.Sehir },
                            PostalZone = new PostalZoneType { Value = firmaParametre.PostaKodu },
                            Country = new CountryType { Name = new NameType1 { Value = firmaParametre.Ulke }, }
                        },
                        WebsiteURI = new WebsiteURIType { Value = firmaParametre.WebSitesi },
                        Contact = new ContactType { ElectronicMail = new ElectronicMailType { Value = firmaParametre.Email }, Telephone = new TelephoneType { Value = firmaParametre.TelefonNo } },
                        PartyTaxScheme = new PartyTaxSchemeType { TaxScheme = new TaxSchemeType { Name = new NameType1 { Value = firmaParametre.VergiDairesi } } }
                    }
                },
                AccountingCustomerParty = new CustomerPartyType
                {
                    // MÜŞTERİ TC Girilir..
                    Party = new PartyType
                    {
                        PartyIdentification = new[] { new PartyIdentificationType { ID = new IDType { schemeID = "TCKN", Value = cariHesap.TcKimlikNo } } },
                        PartyName = new PartyNameType { Name = new NameType1 { Value = cariHesap.Firma } },
                        PostalAddress = new AddressType //(1/2) 12. Video 8. dk
                        {
                            Room = new RoomType { Value = "3" }, // Cari Hesap Parametreleri
                            BuildingName = new BuildingNameType { Value = "DENEME" },
                            BuildingNumber = new BuildingNumberType { Value = "3435" },
                            CitySubdivisionName = new CitySubdivisionNameType { Value = "Üsküdar" },
                            CityName = new CityNameType { Value = cariHesap.Sehir },
                            PostalZone = new PostalZoneType { Value = "34954" },
                            Country = new CountryType { Name = new NameType1 { Value = "Türkiye" }, }
                        },
                        Contact = new ContactType { ElectronicMail = new ElectronicMailType { Value = cariHesap.Email }, Telephone = new TelephoneType { Value = cariHesap.Telefon1 } },
                        Person = new PersonType { FirstName = new FirstNameType { Value = cariHesap.CariHesapTemsilcisi }, FamilyName = new FamilyNameType { Value = "Kavak" } },
                    },
                },
                TaxTotal = new[]
                {
                    new TaxTotalType
                    {
                        TaxAmount = new TaxAmountType { Value = fatura.KdvTutari },
                        TaxSubtotal = new[]
                        {
                            new TaxSubtotalType
                            {
                                TaxableAmount = new TaxableAmountType { currencyID = currencyId, Value = fatura.KdvHaricTutar + fatura.IskontoTutari },
                                TaxAmount = new TaxAmountType { currencyID = currencyId, Value = fatura.KdvTutari },
                                CalculationSequenceNumeric = new CalculationSequenceNumericType { Value = 1 }, // Vergi Sayısı
                                TransactionCurrencyTaxAmount = new TransactionCurrencyTaxAmountType { currencyID = currencyId, Value = fatura.KdvTutari },
                                TaxCategory = new TaxCategoryType
                                {
                                    TaxScheme = new TaxSchemeType
                                    {
                                        Name = new NameType1 { Value = "KDV" },
                                        TaxTypeCode = new TaxTypeCodeType { Value = "0015" },
                                    }
                                }
                            }
                        }
                    }
                },
                LegalMonetaryTotal = new MonetaryTotalType
                {
                    LineExtensionAmount = new LineExtensionAmountType { Value = fatura.KdvHaricTutar },
                    TaxExclusiveAmount = new TaxExclusiveAmountType { Value = fatura.KdvHaricTutar + fatura.IskontoTutari },
                    TaxInclusiveAmount = new TaxInclusiveAmountType { Value = fatura.ToplamTutar },
                    AllowanceTotalAmount = new AllowanceTotalAmountType { Value = fatura.IskontoTutari },
                    PayableAmount = new PayableAmountType { Value = fatura.ToplamTutar },
                },
                InvoiceLine = FaturaHareketleri(),
            };
            var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true };
            var ms = new MemoryStream();
            var writer = XmlWriter.Create(ms, settings);
            var srl = new XmlSerializer(invoice.GetType());
            srl.Serialize(writer, invoice, XmlNameSpace());
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            var srRead = new StreamReader(ms);
            var readXml = srRead.ReadToEnd();
            string filePath = $@"{Application.StartupPath}\EArsivFaturalar";
            var path = Path.Combine(filePath + $@"\{invoice.ID.Value}.xml");


            void EFaturaOlustur()
            {
                using (var sWriter = new StreamWriter(path, false, Encoding.UTF8))
                {
                    sWriter.Write(readXml);
                    sWriter.Close();
                }
            }

            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            if (!File.Exists(filePath + $@"\{invoice.ID.Value}.xml"))
                EFaturaOlustur();
            else
                if (MessageBox.Show($@"{invoice.ID.Value}.xml dosyası daha önce oluşturulmuş. Yeniden Oluşturulsun Mu?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                EFaturaOlustur();

            if (MessageBox.Show($@"Fatura Oluşturma İşlemi Başarılı Bir Şekilde Tamamlandı. Faturalar '{filePath}' Konumunda Oluşturuldu. Konumu Açmak İstiyor Musunuz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes) return;

            Process.Start(filePath);

            XmlSerializerNamespaces XmlNameSpace()
            {
                var ns = new XmlSerializerNamespaces();

                ns.Add("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
                ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                ns.Add("xades", "http://uri.etsi.org/01903/v1.3.2");
                ns.Add("udt", "urn:oasis:names:specification:ubl:schema:xsd:CoreComponentParameters-2");
                ns.Add("ubltr", "urn:oasis:names:specification:ubl:schema:xsd:TurkishCustomizationExtensionComponents");
                ns.Add("qdt", "urn:oasis:names:specification:ubl:schema:xsd:QualifiedDatatypes-2");
                ns.Add("ext", "urn: oasis:names: specification: ubl: schema: xsd: CommonExtensionComponents - 2");
                ns.Add("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
                ns.Add("ccts", "urn:un:unece:uncefact:data:draft:UnqualifiedDataTypesSchemaModule:2");
                ns.Add("ds", "http://www.w3.org/2000/09/xmldsig#");

                return ns;
            }
        }

        protected override void EFaturaOlustur()
        {
            var source = urunBilgileriTable.Tablo.DataController.ListSource.Cast<UrunBilgileriL>();
            FaturaOlustur((SatisFaturaS)OldEntity, source.ToList());
        }

        protected override void BaskiOnizleme()
        {
            string GetDocumentText(string xmlFilePath, string xsltFilePath)
            {
                var xslTransform = new XslCompiledTransform();
                var stringWriter = new StringWriter();
                var reader = XmlReader.Create(xsltFilePath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse });
                xslTransform.Load(reader);
                xslTransform.Transform(xmlFilePath, null, stringWriter);

                return stringWriter.ToString();
            }
            // DOSYA KONTROLÜ YAP
            var xml = $@"{Application.StartupPath}\EArsivFaturalar\{txtKod.Text}.xml";
            var xslt = $@"{Application.StartupPath}\general.xslt";
            var frm = new EFaturaGoruntuleyiciEditForm(GetDocumentText(xml, xslt));
            frm.ShowDialog();
        }
    }
}