using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace PdfiumEditor
{
    /// <summary>
    /// Provides functionality to render a PDF document.
    /// </summary>
    public class PdfDocument : IDisposable
    {
        private bool duplex = false; 
        public int[] duplexblankpages;
        public bool showoverlay = false;
        public int add_window_l = 61;
        public int add_window_t = 132; 
        public int add_window_w = 182;
        public int add_window_h = 113;
        private bool _disposed;
        private PdfFile _file;

        public void storeduplexblanks(int[] newarray)
        {
            duplexblankpages = (int[])newarray.Clone();
        }

        public void setduplex()
        {
            if (!duplex)
            {
                foreach (int duppage in duplexblankpages)
                {
                    NativeMethods.FPDFPage_New(_file._document, duppage, 595, 842);
                    updatepageinfo();
                }
                duplex = true;
            }
           
        }

        public void setsimplex()
        {
            if (duplex)
            {
                for (int i = duplexblankpages.Length - 1; i > -1; i--)
                {
                    int duppage = duplexblankpages[i];
                    NativeMethods.FPDFPage_Delete(_file._document, duppage);
                    updatepageinfo();
                }
                duplex = false;
            }
        }

        public byte[] BlackPixel()
        {
            return new byte[]{0, 0, 0, 0};
        }
        public byte[] WhitePixel()
        {
            return new byte[] { 255, 255, 255, 0 };
        }

        public bool image_at_point(int pageno,float xpos,float ypos)
        {
            bool retval = false;
            //switch the origin
            ypos = this.PageSizes[pageno].Height - ypos; 
            IntPtr ppage = NativeMethods.FPDF_LoadPage(_file._document, pageno);
            int objcount = NativeMethods.FPDFPage_CountObject(ppage);
            for (int i = 0; i < objcount; i++)
            {
                IntPtr obj = NativeMethods.FPDFPage_GetObject(ppage, i);
                int objtype = NativeMethods.FPDFPageObj_GetObjectType(obj);
                //Console.WriteLine("ObjectType: " + objtype.ToString());
                if (objtype == 3)
                {
                    //Console.WriteLine("xpos: "+xpos.ToString());
                    //Console.WriteLine("ypos: "+ypos.ToString());
                    unsafe
                    {
                        float posl = 0, posb = 0, posr = 0, post = 0;
                        NativeMethods.FPDFPageObj_GetBBox(obj, &posl, &posb, &posr, &post);
                        //Console.WriteLine(posl.ToString() + " : " + posr.ToString() + " : " + post.ToString() + " : " + posb.ToString());
                        if (xpos > posl && xpos < posr && ypos > posb && ypos < post)
                        {
                            retval = true;
                            break;
                        }
                    }

                }

            }
            return retval;
        }

        public void move_Objects_in_Rect(int pageno,double left,double top,double right,double bottom,double xmove,double ymove)
        {
            IntPtr ppage = NativeMethods.FPDF_LoadPage(_file._document, pageno);
            NativeMethods.FPDFPageObj_TransformRect(ppage, left, top, right, bottom, 1, 0, 0, 1, xmove, ymove);
            
            //NativeMethods.FPDFPage_GenerateContent(ppage);
            return;
        }

        public bool add_PDF_Object()
        {
            IntPtr newobj = NativeMethods.FPDFPageObj_NewImgeObj(_file._document);
            IntPtr Bmap = NativeMethods.FPDFBitmap_Create(16, 16, 0);
            IntPtr ppage = NativeMethods.FPDF_LoadPage(_file._document, 0);
            NativeMethods.FPDFPage_InsertObject(ppage, newobj);
            NativeMethods.FPDFImageObj_SetBitmap(ppage, 1, newobj, Bmap);
            //unsafe
            //{
            //    IntPtr newobj = NativeMethods.FPDFPageObj_NewImgeObj(_file._document);
            //    IntPtr Bmap = NativeMethods.FPDFBitmap_Create(16, 16, 0);
            //        byte* bytebuff = (byte*)NativeMethods.FPDFBitmap_GetBuffer(Bmap);
            //        //byte[] inputbuff = new byte[1024];
            //        byte[] whitepix = WhitePixel();
            //        byte[] blackpix = BlackPixel();
            //        for (int i = 0; i < 1024; i++)
            //        {
            //            bytebuff[i] = whitepix[0];
            //            i++;
            //            bytebuff[i] = whitepix[1];
            //            i++;
            //            bytebuff[i] = whitepix[2];
            //            i++;
            //            bytebuff[i] = whitepix[3];
            //            i++;
            //            bytebuff[i] = blackpix[0];
            //            i++;
            //            bytebuff[i] = blackpix[1];
            //            i++;
            //            bytebuff[i] = blackpix[2];
            //            i++;
            //            bytebuff[i] = blackpix[3];
            //        }
                


            //    //fixed (byte* pSource = inputbuff)
            //    //{
            //    //    // Set the starting points in source and target for the copying. 
            //    //    byte* ps = pSource + 0;
            //    //    byte* pt = bytebuff + 0;

            //    //    // Copy the specified number of bytes from source to target. 
            //    //    for (int i = 0; i < 1024; i++)
            //    //    {
            //    //        *pt = *ps;
            //    //        pt++;
            //    //        ps++;
            //    //    }
            //    //}
            //    //IntPtr ppage = NativeMethods.FPDF_LoadPage(_file._document, 1);
            //    //NativeMethods.FPDFImageObj_SetBitmap(ppage, 1, newobj, Bmap);
            //    //NativeMethods.FPDFPage_InsertObject(ppage, newobj);
            //}

            
            
            
            //NativeMethods.FPDFPage_GenerateContent(ppage);

            return true;
        }

        // for when we need to remove an insert
        public void removepages(int[] newarray)
        {
            bool changedduplex = false;
            if (!duplex)
            {
               setduplex();
               changedduplex = true;
            }
            for (int i = newarray.Length - 1; i > -1; i--)
            {
                NativeMethods.FPDFPage_Delete(_file._document, i);
                updatepageinfo();
            }
            if (changedduplex)
            {
                setsimplex();
            }
        }

        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided path.
        /// </summary>
        /// <param name="path"></param>
        public static PdfDocument Load(string path)
        {
            return new PdfDocument(path);
        }

        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided stream.
        /// </summary>
        /// <param name="stream"></param>
        public static PdfDocument Load(Stream stream)
        {
            return new PdfDocument(stream);
        }

        /// <summary>
        /// Number of pages in the PDF document.
        /// </summary>
        public int PageCount
        {
            get { return PageSizes.Count; }
        }

        /// <summary>
        /// Size of each page in the PDF document.
        /// </summary>
        public IList<SizeF> PageSizes { get; private set; }

        private PdfDocument(Stream stream)
            : this(PdfFile.Create(stream))
        {
        }

        private PdfDocument(string path)
            : this(File.OpenRead(path))
        {
        }

        private PdfDocument(PdfFile file)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            _file = file;

            var pageSizes = file.GetPDFDocInfo();
            if (pageSizes == null)
                throw new Win32Exception();

            PageSizes = new ReadOnlyCollection<SizeF>(pageSizes);
        }

        public void SaveAs(String newfname)
        {
            NativeMethods.FPDF_SaveToFile(_file._document, new StringBuilder(newfname), 0);
        }

        public int GetVersion()
        {
            //String vernum = "";
            int storeversion = 0;
            NativeMethods.FPDF_GetFileVersion(_file._document, ref storeversion);

            return storeversion;
        }

        /// <summary>
        /// Renders a page of the PDF document and return a bitmap.
        /// </summary>
        public Bitmap RenderBitmap(int page,Bitmap bmp)
        {
            //Bitmap bmp = new Bitmap(100, 100);
            Graphics gr = Graphics.FromImage(bmp);
            gr.Clear(Color.White);
            gr.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            //gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Render(page, gr, gr.DpiX, gr.DpiY, Rectangle.FromLTRB(0, 0, bmp.Width, bmp.Height), false);
            return bmp;
        }

        /// <summary>
        /// Renders a page of the PDF document to the provided graphics instance.
        /// </summary>
        /// <param name="page">Number of the page to render.</param>
        /// <param name="graphics">Graphics instance to render the page on.</param>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="bounds">Bounds to render the page in.</param>
        /// <param name="forPrinting">Render the page for printing.</param>
        public void Render(int page, Graphics graphics, float dpiX, float dpiY, Rectangle bounds, bool forPrinting)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            float graphicsDpiX = graphics.DpiX;
            float graphicsDpiY = graphics.DpiY;

            var dc = graphics.GetHdc();

            try
            {
                if ((int)graphicsDpiX != (int)dpiX || (int)graphicsDpiY != (int)dpiY)
                {
                    var transform = new NativeMethods.XFORM
                    {
                        eM11 = graphicsDpiX / dpiX,
                        eM22 = graphicsDpiY / dpiY
                    };

                    NativeMethods.SetGraphicsMode(dc, NativeMethods.GM_ADVANCED);
                    NativeMethods.ModifyWorldTransform(dc, ref transform, NativeMethods.MWT_LEFTMULTIPLY);
                }

                bool success = _file.RenderPDFPageToDC(
                    page,
                    dc,
                    (int)dpiX, (int)dpiY,
                    bounds.X, bounds.Y, bounds.Width, bounds.Height,
                    true /* fitToBounds */,
                    true /* stretchToBounds */,
                    true /* keepAspectRatio */,
                    true /* centerInBounds */,
                    true /* autoRotate */,
                    forPrinting
                );

                if (!success)
                    throw new Win32Exception();
            }
            finally
            {
                graphics.ReleaseHdc(dc);
            }
        }

        public bool Append_PDF(PdfDocument src_doc)
        {
            bool result;
            result = _file.Append_PDF(src_doc._file._document, this.PageCount);
            // update the pages info
            updatepageinfo();

            return result;
        }

        private void updatepageinfo()
        {
            // update the pages info
            var pageSizes = _file.GetPDFDocInfo();
            if (pageSizes == null)
                throw new Win32Exception();
            PageSizes = new ReadOnlyCollection<SizeF>(pageSizes);
        }

        public String Extract_text(int pageno, double left, double top, double right, double bottom, bool newtype)
        {
            String extext = "";
            extext = _file.Extract_text(pageno, left, top, right, bottom, newtype);
            return extext;
        }

        /// <summary>
        /// Save the PDF document to the specified location.
        /// </summary>
        /// <param name="path">Path to save the PDF document to.</param>
        public void Save(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            using (var stream = File.Create(path))
            {
                Save(stream);
            }
        }

        /// <summary>
        /// Save the PDF document to the specified location.
        /// </summary>
        /// <param name="stream">Stream to save the PDF document to.</param>
        public void Save(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            _file.Save(stream);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_file != null)
                {
                    _file.Dispose();
                    _file = null;
                }

                _disposed = true;
            }
        }
    }
}
