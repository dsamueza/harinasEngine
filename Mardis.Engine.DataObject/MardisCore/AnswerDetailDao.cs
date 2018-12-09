using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.Framework.Resources;
using Mardis.Engine.Web.ViewModel.TaskViewModels;
using Microsoft.EntityFrameworkCore;

namespace Mardis.Engine.DataObject.MardisCore
{
    public class AnswerDetailDao : ADao
    {
        public AnswerDetailDao(MardisContext mardisContext) : base(mardisContext)
        {
        }

        public AnswerDetail GetAnswerDetail(Guid idQuestionDetail, int copyNumber, Guid idAnswer, Guid idAccount)
        {
            return Context.AnswerDetails
                .FirstOrDefault(
                    a => a.IdAnswer == idAnswer &&
                    a.CopyNumber == copyNumber &&
                    a.IdQuestionDetail == idQuestionDetail &&
                    a.StatusRegister == CStatusRegister.Active &&
                    a.Answer.IdAccount == idAccount);
        }
        public List<MytaskAnwerDetailSecondModel> GetAnswerDetailSecond(Guid idAnswerDetail)
        {

            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<AnswerDetailSecondLevel, MytaskAnwerDetailSecondModel>();
            });
            var itemResult = new List<MytaskAnwerDetailSecondModel>();
            var _data = Context.AnswerDetailSecondLevels.Where(x => x.AnswerDetailId.Equals(idAnswerDetail)).ToList();
            itemResult = Mapper.Map<List<MytaskAnwerDetailSecondModel>>(_data);
            var _concept = Context._AnswerDetailSecondLevelConcepts.Where(x => x.AnswerDetailSecondLevelid.Equals(_data.First().Id)).Select(z=>z.IdquestionDetail.ToString());
            if(_concept.Count()>0)
            itemResult.First().QuestionComplete =_concept.ToList();

            return itemResult;

        }

        public AnswerDetail GetAnswerDetail(int copyNumber, Guid idAnswer, Guid idAccount)
        {
            return Context.AnswerDetails
                
                .FirstOrDefault(
                    a => a.IdAnswer == idAnswer &&
                    a.CopyNumber == copyNumber &&
                    a.StatusRegister == CStatusRegister.Active &&
                    a.Answer.IdAccount == idAccount);
        }

    }
}
