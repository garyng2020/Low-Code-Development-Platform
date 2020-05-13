
import { getAsyncTypes } from '../helpers/actionType'
import * as AdmUsrGroupService from '../services/AdmUsrGroupService'
import { RintagiScreenRedux, initialRintagiScreenReduxState } from './_ScreenReducer'
class AdmUsrGroupRedux extends RintagiScreenRedux {
  allowTmpDtl = false;
  constructor() {
    super();
    this.ActionApiNameMapper = {
      'GET_SEARCH_LIST': 'GetAdmUsrGroup5List',
      'GET_MST': 'GetAdmUsrGroup5ById',
      'GET_DTL_LIST': 'GetAdmUsrGroup5DtlById',
    }
    this.ScreenDdlDef = [
      { columnName: 'RowAuthorityId7', payloadDdlName: 'RowAuthorityId7List', keyName: 'RowAuthorityId7', labelName: 'RowAuthorityId7Text', forMst: true, isAutoComplete: false, apiServiceName: 'GetRowAuthorityId7List', actionTypeName: 'GET_DDL_RowAuthorityId7' },
      { columnName: 'CompanyId7', payloadDdlName: 'CompanyId7List', keyName: 'CompanyId7', labelName: 'CompanyId7Text', forMst: true, isAutoComplete: false, apiServiceName: 'GetCompanyId7List', actionTypeName: 'GET_DDL_CompanyId7' },
      { columnName: 'CompanyId58', payloadDdlName: 'CompanyId58List', keyName: 'CompanyId58', labelName: 'CompanyId58Text', forMst: false, isAutoComplete: false, apiServiceName: 'GetCompanyId58List', actionTypeName: 'GET_DDL_CompanyId58' },
      { columnName: 'ProjectId58', payloadDdlName: 'ProjectId58List', keyName: 'ProjectId58', labelName: 'ProjectId58Text', forMst: false, isAutoComplete: false, apiServiceName: 'GetProjectId58List', actionTypeName: 'GET_DDL_ProjectId58' },
      { columnName: 'SystemId58', payloadDdlName: 'SystemId58List', keyName: 'SystemId58', labelName: 'SystemId58Text', forMst: false, isAutoComplete: false, apiServiceName: 'GetSystemId58List', actionTypeName: 'GET_DDL_SystemId58' },
      { columnName: 'SysRowAuthorityId58', payloadDdlName: 'SysRowAuthorityId58List', keyName: 'SysRowAuthorityId58', labelName: 'SysRowAuthorityId58Text', forMst: false, isAutoComplete: false, apiServiceName: 'GetSysRowAuthorityId58List', actionTypeName: 'GET_DDL_SysRowAuthorityId58' },
    ]
    this.ScreenOnDemandDef = [

    ]
    this.ScreenDocumentDef = [

    ]
    this.ScreenCriDdlDef = [

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
  GetScreenName() { return 'AdmUsrGroup' }
  GetMstKeyColumnName(isUnderlining = false) { return isUnderlining ? 'UsrGroupId' : 'UsrGroupId7'; }
  GetDtlKeyColumnName(isUnderlining = false) { return isUnderlining ? 'UsrGroupAuthId' : 'UsrGroupAuthId58'; }
  GetPersistDtlName() { return this.GetScreenName() + '_Dtl'; }
  GetPersistMstName() { return this.GetScreenName() + '_Mst'; }
  GetWebService() { return AdmUsrGroupService; }
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
      UsrGroupAuthId58: null,
      CompanyId58: null,
      ProjectId58: null,
      Filler: null,
      FillerBtn: null,
      MoreInfo: null,
      SystemId58: null,
      SysRowAuthorityId58: null,
    }
  }
  ExpandMst(mst, state, copy) {
    return {
      ...mst,
      key: Date.now(),
      UsrGroupId7: copy ? null : mst.UsrGroupId7,
    }
  }
  ExpandDtl(dtlList, copy) {
    if (!copy) return dtlList;
    else if (!this.allowTmpDtl) return [];
    else {
      const now = Date.now();
      return dtlList.map((v, i) => {
        return {
          ...v,
          UsrGroupId7: null,
          UsrGroupAuthId58: null,
          TmpKeyId: now + i,
        }
      })
    };
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

    || state.ScreenCriteria.SearchStr;
}

export default new AdmUsrGroupRedux()
