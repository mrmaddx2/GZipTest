using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SkillsTest.GZipTest.Core
{
    public class MrSource : MrAbstractBlock
    {
        #region Delegates

        protected delegate void FillingActionHandler();
        #endregion

        
        protected InputFile inputFile;
        

        public void Post(string value)
        {
            try
            {
                this.inputFile = new InputFile(value);

                this.Status = ProjectStatusEnum.InProgress;

                FillingActionHandler fillingAction = this.Filling;

                this.Status = ProjectStatusEnum.InProgress;

                fillingAction.BeginInvoke(null, null);
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Блок чтения данных из файла {0}", value), exception);
            }
        }


        protected virtual void Filling()
        {
            try
            {
                PieceOf newPiece;
                while ((newPiece = this.inputFile.Fetch(inputFragmentSize: null)) != null && this.Status == ProjectStatusEnum.InProgress)
                {
                    this.AddToBuffer(newPiece);
                }

                this.PostDone();
            }
            catch (Exception exception)
            {
                this.PostError(exception);
            }
        }
    }
}
