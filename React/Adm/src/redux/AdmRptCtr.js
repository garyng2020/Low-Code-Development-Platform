
import { getAsyncTypes } from '../helpers/actionType'
import * as AdmRptCtrService from '../services/AdmRptCtrService'
import { RintagiScreenRedux, initialRintagiScreenReduxState } from './_ScreenReducer'
class AdmRptCtrRedux extends RintagiScreenRedux {
  allowTmpDtl = false;
  constructor() {
    super();
    this.ActionApiNameMapper = {
      'GET_SEARCH_LIST': 'GetAdmRptCtr90List',
      'GET_MST': 'GetAdmRptCtr90ById',
      'GET_DTL_LIST': 'GetAdmRptCtr90DtlById',
    }
    this.ScreenDdlDef = [
      { columnName: 'ReportId161', payloadDdlName: 'ReportId161List', keyName: 'ReportId161', labelName: 'ReportId161Text', forMst: true, isAutoComplete: true, apiServiceName: 'GetReportId161List', actionTypeName: 'GET_DDL_ReportId161' },
      { columnName: 'PRptCtrId161', payloadDdlName: 'PRptCtrId161List', keyName: 'PRptCtrId161', labelName: 'PRptCtrId161Text', forMst: true, isAutoComplete: true, apiServiceName: 'GetPRptCtrId161List', actionTypeName: 'GET_DDL_PRptCtrId161' },
      { columnName: 'RptElmId161', payloadDdlName: 'RptElmId161List', keyName: 'RptElmId161', labelName: 'RptElmId161Text', forMst: true, isAutoComplete: true, apiServiceName: 'GetRptElmId161List', actionTypeName: 'GET_DDL_RptElmId161' },
      { columnName: 'RptCelId161', payloadDdlName: 'RptCelId161List', keyName: 'RptCelId161', labelName: 'RptCelId161Text', forMst: true, isAutoComplete: true, apiServiceName: 'GetRptCelId161List', actionTypeName: 'GET_DDL_RptCelId161' },
      { columnName: 'RptStyleId161', payloadDdlName: 'RptStyleId161List', keyName: 'RptStyleId161', labelName: 'RptStyleId161Text', forMst: true, isAutoComplete: true, apiServiceName: 'GetRptStyleId161List', actionTypeName: 'GET_DDL_RptStyleId161' },
      { columnName: 'RptCtrTypeCd161', payloadDdlName: 'RptCtrTypeCd161List', keyName: 'RptCtrTypeCd161', labelName: 'RptCtrTypeCd161Text', forMst: true, isAutoComplete: false, apiServiceName: 'GetRptCtrTypeCd161List', actionTypeName: 'GET_DDL_RptCtrTypeCd161' },
      { columnName: 'CtrVisibility161', payloadDdlName: 'CtrVisibility161List', keyName: 'CtrVisibility161', labelName: 'CtrVisibility161Text', forMst: true, isAutoComplete: false, apiServiceName: 'GetCtrVisibility161List', actionTypeName: 'GET_DDL_CtrVisibility161' },
      { columnName: 'CtrToggle161', payloadDdlName: 'CtrToggle161List', keyName: 'CtrToggle161', labelName: 'CtrToggle161Text', forMst: true, isAutoComplete: true, apiServiceName: 'GetCtrToggle161List', filterByMaster: true, filterByColumnName: 'ReportId161', actionTypeName: 'GET_DDL_CtrToggle161' },
      { columnName: 'CtrGrouping161', payloadDdlName: 'CtrGrouping161List', keyName: 'CtrGrouping161', labelName: 'CtrGrouping161Text', forMst: true, isAutoComplete: true, apiServiceName: 'GetCtrGrouping161List', filterByMaster: true, filterByColumnName: 'ReportId161', actionTypeName: 'GET_DDL_CtrGrouping161' },
    ]
    this.ScreenOnDemandDef = [

    ]
    this.ScreenDocumentDef = [

    ]
    this.ScreenCriDdlDef = [
      { columnName: 'ReportId10', payloadDdlName: 'ReportId10List', isAutoComplete: true, apiServiceName: 'GetScreenCriReportId10List', actionTypeName: 'GET_DDL_CRIReportId10' },
      { columnName: 'CtrValue20', payloadDdlName: '', keyName: '', labelName: '', isCheckBox:false, isAutoComplete: false, apiServiceName: '', actionTypeName: 'GET_CtrValue20' },
    ]
    this.SearchActions = {
      ...[...this.ScreenDdlDef].reduce((a, v) => { a['Search' + v.columnName] = this.MakeSearchAction(v); return a; }, {}),
      ...[...this.ScreenCriDdlDef].reduce((a, v) => { a['SearchCri' + v.columnName] = this.MakeSearchAction(v); return a; }, {}),
      ...[...this.ScreenOnDemandDef].filter(f => f.type !== 'DocList' && f.type !== 'RefColumn').reduce((a, v) => { a['Get' + v.columnName] = this.MakeGetColumnOnDemandAction(v); return a; }, {}),
      ...[...this.ScreenOnDemandDef].filter(f => f.type === 'RefColumn').reduce((a, v) => { a['Get' + v.columnName] = this.MakeGetRefColumnOnDemandAction(v); return a; }, {}),
      ...this.MakePullUpOnDemandAction([...this.ScreenOnDemandDef].filter(f => f.type === 'RefColumn').reduce((a, v) => { a['GetRef' + v.refColumnName] = { dependents: [...((a['GetRef' + v.refColumnName] || {}).dependents || []), v] }; return a; }, {})),
      ...[...this.ScreenOnDemandDef].filter(f => f.type === 'DocList').reduce((a, v) => { a['Get' + v.columnName] = this.MakeGetDocumentListAction(v); return a; }, {}),
    }
    this.OnDemandActions = {
      ...[...this.ScreenDocumentDef].filter(f => f.type === 'Get').reduce((a, v) => { a['Get' + v.columnName + 'Content'] = this.MakeGetDocumentContentAction(v); return a; }, {}),
      ...[...this.ScreenDocumentDef].filter(f => f.type === 'Add').reduce((a, v) => { a['Add' + v.columnName + 'Content'] = this.MakeAddDocumentContentAction(v); return a; }, {}),
      ...[...this.ScreenDocumentDef].filter(f => f.type === 'Del').reduce((a, v) => { a['Del' + v.columnName + 'Content'] = this.MakeDelDocumentContentAction(v); return a; }, {}),
    }
    this.ScreenDdlSelectors = this.ScreenDdlDef.reduce((a, v) => { a[v.columnName] = this.MakeDdlSelectors(v); return a; }, {})
    this.ScreenCriDdlSelectors = this.ScreenCriDdlDef.reduce((a, v) => { a[v.columnName] = this.MakeCriDdlSelectors(v); return a; }, {})
    this.actionReducers = this.MakeActionReducers();
  }
  GetScreenName() { return 'AdmRptCtr' }
  GetMstKeyColumnName(isUnderlining = false) { return isUnderlining ? 'RptCtrId' : 'RptCtrId161'; }
  GetDtlKeyColumnName(isUnderlining = false) { return isUnderlining ? '' : ''; }
  GetPersistDtlName() { return this.GetScreenName() + '_Dtl'; }
  GetPersistMstName() { return this.GetScreenName() + '_Mst'; }
  GetWebService() { return AdmRptCtrService; }
  GetReducerActionTypePrefix() { return this.GetScreenName(); };
  GetActionType(actionTypeName) { return getAsyncTypes(this.GetReducerActionTypePrefix(), actionTypeName); }
  GetInitState() {
    return {
      ...initialRintagiScreenReduxState,
      Label: {
        ...initialRintagiScreenReduxState.Label,
      }
    }
  };

  GetDefaultDtl(state) {
    return (state || {}).NewDtl ||
    {

    }
  }
  ExpandMst(mst, state, copy) {
    return {
      ...mst,
      key: Date.now(),
      RptCtrId161: copy ? null : mst.RptCtrId161,
    }
  }
  ExpandDtl(dtlList, copy) {
    return dtlList;
  }

  SearchListToSelectList(state) {
    const searchList = ((state || {}).SearchList || {}).data || [];
    return searchList
      .map((v, i) => {
        return {
          key: v.key || null,
          value: v.labelL || v.label || ' ',
          label: v.labelL || v.label || ' ',
          labelR: v.labelR || ' ',
          detailR: v.detailR || ' ',
          detail: v.detail || ' ',
          idx: i,
          isSelected: v.isSelected,
        }
      })
  }
}

/* ReactRule: Redux Custom Function */

/* ReactRule End: Redux Custom Function */

/* helper functions */

export function ShowMstFilterApplied(state) {
  return !state
    || !state.ScreenCriteria
    || (state.ScreenCriteria.ReportId10 || {}).LastCriteria
    || (state.ScreenCriteria.CtrValue20 || {}).LastCriteria
    || state.ScreenCriteria.SearchStr;
}

export default new AdmRptCtrRedux()
