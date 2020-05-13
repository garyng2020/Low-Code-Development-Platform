
import React, { Component } from 'react';
import { bindActionCreators } from 'redux';
import { connect } from 'react-redux';
import { Prompt, Redirect } from 'react-router';
import { Button, Row, Col, ButtonToolbar, ButtonGroup, DropdownItem, DropdownMenu, DropdownToggle, UncontrolledDropdown, Nav, NavItem, NavLink } from 'reactstrap';
import { Formik, Field, Form } from 'formik';
import DocumentTitle from 'react-document-title';
import classNames from 'classnames';
import LoadingIcon from 'mdi-react/LoadingIcon';
import CheckIcon from 'mdi-react/CheckIcon';
import DatePicker from '../../components/custom/DatePicker';
import NaviBar from '../../components/custom/NaviBar';
import DropdownField from '../../components/custom/DropdownField';
import AutoCompleteField from '../../components/custom/AutoCompleteField';
import ListBox from '../../components/custom/ListBox';
import { default as FileInputFieldV1 } from '../../components/custom/FileInputV1';
import { default as FileInputField } from '../../components/custom/FileInput';
import RintagiScreen from '../../components/custom/Screen';
import ModalDialog from '../../components/custom/ModalDialog';
import { showNotification } from '../../redux/Notification';
import { registerBlocker, unregisterBlocker } from '../../helpers/navigation'
import { isEmptyId, getAddDtlPath, getAddMstPath, getEditDtlPath, getEditMstPath, getNaviPath, getDefaultPath, decodeEmbeddedFileObjectFromServer } from '../../helpers/utils'
import { toMoney, toLocalAmountFormat, toLocalDateFormat, toDate, strFormat, formatContent } from '../../helpers/formatter';
import { setTitle, setSpinner } from '../../redux/Global';
import { RememberCurrent, GetCurrent } from '../../redux/Persist'
import { getNaviBar } from './index';
import AdmRptElmReduxObj, { ShowMstFilterApplied } from '../../redux/AdmRptElm';
import * as AdmRptElmService from '../../services/AdmRptElmService';
import { getRintagiConfig } from '../../helpers/config';
import Skeleton from 'react-skeleton-loader';
import ControlledPopover from '../../components/custom/ControlledPopover';
import log from '../../helpers/logger';

class MstRecord extends RintagiScreen {
  constructor(props) {
    super(props);
    this.GetReduxState = () => (this.props.AdmRptElm || {});
    this.blocker = null;
    this.titleSet = false;
    this.MstKeyColumnName = 'RptElmId160';
    this.SystemName = 'FintruX';
    this.confirmUnload = this.confirmUnload.bind(this);
    this.hasChangedContent = false;
    this.setDirtyFlag = this.setDirtyFlag.bind(this);
    this.AutoCompleteFilterBy = (option, props) => { return true };
    this.OnModalReturn = this.OnModalReturn.bind(this);
    this.ValidatePage = this.ValidatePage.bind(this);
    this.SavePage = this.SavePage.bind(this);
    this.FieldChange = this.FieldChange.bind(this);
    this.DateChange = this.DateChange.bind(this);
    this.StripEmbeddedBase64Prefix = this.StripEmbeddedBase64Prefix.bind(this);
    this.DropdownChangeV1 = this.DropdownChangeV1.bind(this);
    this.FileUploadChangeV1 = this.FileUploadChangeV1.bind(this);
    this.mobileView = window.matchMedia('(max-width: 1200px)');
    this.mediaqueryresponse = this.mediaqueryresponse.bind(this);
    this.SubmitForm = ((submitForm, options = {}) => {
      const _this = this;
      return (evt) => {
        submitForm();
      }
    }
    );
    this.state = {
      submitting: false,
      ScreenButton: null,
      key: '',
      Buttons: {},
      ModalColor: '',
      ModalTitle: '',
      ModalMsg: '',
      ModalOpen: false,
      ModalSuccess: null,
      ModalCancel: null,
      isMobile: false,
    }
    if (!this.props.suppressLoadPage && this.props.history) {
      RememberCurrent('LastAppUrl', (this.props.history || {}).location, true);
    }

    if (!this.props.suppressLoadPage) {
      this.props.setSpinner(true);
    }
  }

  mediaqueryresponse(value) {
    if (value.matches) { // if media query matches
      this.setState({ isMobile: true });
    }
    else {
      this.setState({ isMobile: false });
    }
  }

  ReportId160InputChange() { const _this = this; return function (name, v) { const filterBy = ''; _this.props.SearchReportId160(v, filterBy); } }
  RptStyleId160InputChange() { const _this = this; return function (name, v) { const filterBy = ''; _this.props.SearchRptStyleId160(v, filterBy); } }
  /* ReactRule: Master Record Custom Function */

  /* ReactRule End: Master Record Custom Function */

  /* form related input handling */

  ValidatePage(values) {
    const errors = {};
    const columnLabel = (this.props.AdmRptElm || {}).ColumnLabel || {};
    /* standard field validation */
    if (isEmptyId((values.cReportId160 || {}).value)) { errors.cReportId160 = (columnLabel.ReportId160 || {}).ErrMessage; }
    if (isEmptyId((values.cRptElmTypeCd160 || {}).value)) { errors.cRptElmTypeCd160 = (columnLabel.RptElmTypeCd160 || {}).ErrMessage; }
    if (!values.cElmHeight160) { errors.cElmHeight160 = (columnLabel.ElmHeight160 || {}).ErrMessage; }
    return errors;
  }

  SavePage(values, { setSubmitting, setErrors, resetForm, setFieldValue, setValues }) {
    const errors = [];
    const currMst = (this.props.AdmRptElm || {}).Mst || {};

    /* ReactRule: Master Record Save */

    /* ReactRule End: Master Record Save */

    if (errors.length > 0) {
      this.props.showNotification('E', { message: errors[0] });
      setSubmitting(false);
    }
    else {
      const { ScreenButton, OnClickColumeName } = this;
      this.setState({ submittedOn: Date.now(), submitting: true, setSubmitting: setSubmitting, key: currMst.key, ScreenButton: ScreenButton, OnClickColumeName: OnClickColumeName });
      this.ScreenButton = null;
      this.OnClickColumeName = null;
      this.props.SavePage(
        this.props.AdmRptElm,
        {
          RptElmId160: values.cRptElmId160 || '',
          ReportId160: (values.cReportId160 || {}).value || '',
          RptElmTypeCd160: (values.cRptElmTypeCd160 || {}).value || '',
          RptStyleId160: (values.cRptStyleId160 || {}).value || '',
          ElmHeight160: values.cElmHeight160 || '',
          ElmColumns160: values.cElmColumns160 || '',
          ElmColSpacing160: values.cElmColSpacing160 || '',
          ElmPrintFirst160: values.cElmPrintFirst160 ? 'Y' : 'N',
          ElmPrintLast160: values.cElmPrintLast160 ? 'Y' : 'N',
        },
        [],
        {
          persist: true,
          ScreenButton: (ScreenButton || {}).buttonType,
          OnClickColumeName: OnClickColumeName,
        }
      );
    }
  }
  /* end of form related handling functions */

  /* standard screen button actions */
  SaveMst({ submitForm, ScreenButton }) {
    return function (evt) {
      this.ScreenButton = ScreenButton;
      submitForm();
    }.bind(this);
  }
  SaveCloseMst({ submitForm, ScreenButton, naviBar, redirectTo, onSuccess }) {
    return function (evt) {
      this.ScreenButton = ScreenButton;
      submitForm();
    }.bind(this);
  }
  NewSaveMst({ submitForm, ScreenButton }) {
    return function (evt) {
      this.ScreenButton = ScreenButton;
      submitForm();
    }.bind(this);
  }
  CopyHdr({ ScreenButton, mst, mstId, useMobileView }) {
    const AdmRptElmState = this.props.AdmRptElm || {};
    const auxSystemLabels = AdmRptElmState.SystemLabel || {};
    return function (evt) {
      evt.preventDefault();
      const fromMstId = mstId || (mst || {}).RptElmId160;
      const copyFn = () => {
        if (fromMstId) {
          this.props.AddMst(fromMstId, 'MstRecord', 0);
          /* this is application specific rule as the Posted flag needs to be reset */
          this.props.AdmRptElm.Mst.Posted64 = 'N';
          if (useMobileView) {
            const naviBar = getNaviBar('MstRecord', {}, {}, this.props.AdmRptElm.Label);
            this.props.history.push(getEditMstPath(getNaviPath(naviBar, 'MstRecord', '/'), '_'));
          }
          else {
            if (this.props.onCopy) this.props.onCopy();
          }
        }
        else {
          this.setState({ ModalOpen: true, ModalColor: 'warning', ModalTitle: auxSystemLabels.UnsavedPageTitle || '', ModalMsg: auxSystemLabels.UnsavedPageMsg || '' });
        }
      }
      if (!this.hasChangedContent) copyFn();
      else this.setState({ ModalOpen: true, ModalSuccess: copyFn, ModalColor: 'warning', ModalTitle: auxSystemLabels.UnsavedPageTitle || '', ModalMsg: auxSystemLabels.UnsavedPageMsg || '' });
    }.bind(this);
  }
  DelMst({ naviBar, ScreenButton, mst, mstId }) {
    const AdmRptElmState = this.props.AdmRptElm || {};
    const auxSystemLabels = AdmRptElmState.SystemLabel || {};
    return function (evt) {
      evt.preventDefault();
      const deleteFn = () => {
        const fromMstId = mstId || mst.RptElmId160;
        this.props.DelMst(this.props.AdmRptElm, fromMstId);
      };
      this.setState({ ModalOpen: true, ModalSuccess: deleteFn, ModalColor: 'danger', ModalTitle: auxSystemLabels.WarningTitle || '', ModalMsg: auxSystemLabels.DeletePageMsg || '' });
    }.bind(this);
  }
  /* end of screen button action */

  /* react related stuff */
  static getDerivedStateFromProps(nextProps, prevState) {
    const nextReduxScreenState = nextProps.AdmRptElm || {};
    const buttons = nextReduxScreenState.Buttons || {};
    const revisedButtonDef = super.GetScreenButtonDef(buttons, 'Mst', prevState);
    const currentKey = nextReduxScreenState.key;
    const waiting = nextReduxScreenState.page_saving || nextReduxScreenState.page_loading;
    let revisedState = {};
    if (revisedButtonDef) revisedState.Buttons = revisedButtonDef;

    if (prevState.submitting && !waiting && nextReduxScreenState.submittedOn > prevState.submittedOn) {
      prevState.setSubmitting(false);
    }

    return revisedState;
  }

  confirmUnload(message, callback) {
    const AdmRptElmState = this.props.AdmRptElm || {};
    const auxSystemLabels = AdmRptElmState.SystemLabel || {};
    const confirm = () => {
      callback(true);
    }
    const cancel = () => {
      callback(false);
    }
    this.setState({ ModalOpen: true, ModalSuccess: confirm, ModalCancel: cancel, ModalColor: 'warning', ModalTitle: auxSystemLabels.UnsavedPageTitle || '', ModalMsg: message });
  }

  setDirtyFlag(dirty) {
    /* this is called during rendering but has side-effect, undesirable but only way to pass formik dirty flag around */
    if (dirty) {
      if (this.blocker) unregisterBlocker(this.blocker);
      this.blocker = this.confirmUnload;
      registerBlocker(this.confirmUnload);
    }
    else {
      if (this.blocker) unregisterBlocker(this.blocker);
      this.blocker = null;
    }
    if (this.props.updateChangedState) this.props.updateChangedState(dirty);
    this.SetCurrentRecordState(dirty);
    return true;
  }

  componentDidMount() {
    this.mediaqueryresponse(this.mobileView);
    this.mobileView.addListener(this.mediaqueryresponse) // attach listener function to listen in on state changes
    const isMobileView = this.state.isMobile;
    const useMobileView = (isMobileView && !(this.props.user || {}).desktopView);
    const suppressLoadPage = this.props.suppressLoadPage;
    if (!suppressLoadPage) {
      const { mstId } = { ...this.props.match.params };
      if (!(this.props.AdmRptElm || {}).AuthCol || true) {
        this.props.LoadPage('MstRecord', { mstId: mstId || '_' });
      }
    }
    else {
      return;
    }
  }

  componentDidUpdate(prevprops, prevstates) {
    const currReduxScreenState = this.props.AdmRptElm || {};

    if (!this.props.suppressLoadPage) {
      if (!currReduxScreenState.page_loading && this.props.global.pageSpinner) {
        const _this = this;
        setTimeout(() => _this.props.setSpinner(false), 500);
      }
    }

    const currMst = currReduxScreenState.Mst || {};
    this.SetPageTitle(currReduxScreenState);
    if (prevstates.key !== currMst.key) {
      if ((prevstates.ScreenButton || {}).buttonType === 'SaveClose') {
        const currDtl = currReduxScreenState.EditDtl || {};
        const dtlList = (currReduxScreenState.DtlList || {}).data || [];
        const naviBar = getNaviBar('MstRecord', currMst, currDtl, currReduxScreenState.Label);
        const searchListPath = getDefaultPath(getNaviPath(naviBar, 'MstList', '/'))
        this.props.history.push(searchListPath);
      }
    }
  }

  componentWillUnmount() {
    if (this.blocker) {
      unregisterBlocker(this.blocker);
      this.blocker = null;
    }
    this.mobileView.removeListener(this.mediaqueryresponse);
  }


  render() {
    const AdmRptElmState = this.props.AdmRptElm || {};

    if (AdmRptElmState.access_denied) {
      return <Redirect to='/error' />;
    }

    const screenHlp = AdmRptElmState.ScreenHlp;

    // Labels
    const siteTitle = (this.props.global || {}).pageTitle || '';
    const MasterRecTitle = ((screenHlp || {}).MasterRecTitle || '');
    const MasterRecSubtitle = ((screenHlp || {}).MasterRecSubtitle || '');
    const NoMasterMsg = ((screenHlp || {}).NoMasterMsg || '');

    const screenButtons = AdmRptElmReduxObj.GetScreenButtons(AdmRptElmState) || {};
    const itemList = AdmRptElmState.Dtl || [];
    const auxLabels = AdmRptElmState.Label || {};
    const auxSystemLabels = AdmRptElmState.SystemLabel || {};

    const columnLabel = AdmRptElmState.ColumnLabel || {};
    const authCol = this.GetAuthCol(AdmRptElmState);
    const authRow = (AdmRptElmState.AuthRow || [])[0] || {};
    const currMst = ((this.props.AdmRptElm || {}).Mst || {});
    const currDtl = ((this.props.AdmRptElm || {}).EditDtl || {});
    const naviBar = getNaviBar('MstRecord', currMst, currDtl, screenButtons).filter(v => ((v.type !== 'DtlRecord' && v.type !== 'DtlList') || currMst.RptElmId160));
    const selectList = AdmRptElmReduxObj.SearchListToSelectList(AdmRptElmState);
    const selectedMst = (selectList || []).filter(v => v.isSelected)[0] || {};

    const RptElmId160 = currMst.RptElmId160;
    const ReportId160List = AdmRptElmReduxObj.ScreenDdlSelectors.ReportId160(AdmRptElmState);
    const ReportId160 = currMst.ReportId160;
    const RptElmTypeCd160List = AdmRptElmReduxObj.ScreenDdlSelectors.RptElmTypeCd160(AdmRptElmState);
    const RptElmTypeCd160 = currMst.RptElmTypeCd160;
    const RptStyleId160List = AdmRptElmReduxObj.ScreenDdlSelectors.RptStyleId160(AdmRptElmState);
    const RptStyleId160 = currMst.RptStyleId160;
    const ElmHeight160 = currMst.ElmHeight160;
    const ElmColumns160 = currMst.ElmColumns160;
    const ElmColSpacing160 = currMst.ElmColSpacing160;
    const ElmPrintFirst160 = currMst.ElmPrintFirst160;
    const ElmPrintLast160 = currMst.ElmPrintLast160;

    const { dropdownMenuButtonList, bottomButtonList, hasDropdownMenuButton, hasBottomButton, hasRowButton } = this.state.Buttons;
    const hasActableButtons = hasBottomButton || hasRowButton || hasDropdownMenuButton;

    const isMobileView = this.state.isMobile;
    const useMobileView = (isMobileView && !(this.props.user || {}).desktopView);
    const fileFileUploadOptions = {
      CancelFileButton: 'Cancel',
      DeleteFileButton: 'Delete',
      MaxImageSize: {
        Width: 1024,
        Height: 768,
      },
      MinImageSize: {
        Width: 40,
        Height: 40,
      },
      maxSize: 5 * 1024 * 1024,
    }

    /* ReactRule: Master Render */

    /* ReactRule End: Master Render */

    return (
      <DocumentTitle title={siteTitle}>
        <div>
          <ModalDialog color={this.state.ModalColor} title={this.state.ModalTitle} onChange={this.OnModalReturn} ModalOpen={this.state.ModalOpen} message={this.state.ModalMsg} />
          <div className='account'>
            <div className='account__wrapper account-col'>
              <div className='account__card shadow-box rad-4'>
                {/* {!useMobileView && this.constructor.ShowSpinner(AdmRptElmState) && <div className='panel__refresh'></div>} */}
                {useMobileView && <div className='tabs tabs--justify tabs--bordered-bottom'>
                  <div className='tabs__wrap'>
                    <NaviBar history={this.props.history} navi={naviBar} />
                  </div>
                </div>}
                <p className='project-title-mobile mb-10'>{siteTitle.substring(0, document.title.indexOf('-') - 1)}</p>
                <Formik
                  initialValues={{
                    cRptElmId160: formatContent(RptElmId160 || '', 'TextBox'),
                    cReportId160: ReportId160List.filter(obj => { return obj.key === ReportId160 })[0],
                    cRptElmTypeCd160: RptElmTypeCd160List.filter(obj => { return obj.key === RptElmTypeCd160 })[0],
                    cRptStyleId160: RptStyleId160List.filter(obj => { return obj.key === RptStyleId160 })[0],
                    cElmHeight160: formatContent(ElmHeight160 || '', 'TextBox'),
                    cElmColumns160: formatContent(ElmColumns160 || '', 'TextBox'),
                    cElmColSpacing160: formatContent(ElmColSpacing160 || '', 'TextBox'),
                    cElmPrintFirst160: ElmPrintFirst160 === 'Y',
                    cElmPrintLast160: ElmPrintLast160 === 'Y',
                  }}
                  validate={this.ValidatePage}
                  onSubmit={this.SavePage}
                  key={currMst.key}
                  render={({
                    values,
                    errors,
                    touched,
                    isSubmitting,
                    dirty,
                    setFieldValue,
                    setFieldTouched,
                    handleChange,
                    submitForm
                  }) => (
                      <div>
                        {this.setDirtyFlag(dirty) &&
                          <Prompt
                            when={dirty}
                            message={auxSystemLabels.UnsavedPageMsg || ''}
                          />
                        }
                        <div className='account__head'>
                          <Row>
                            <Col xs={useMobileView ? 9 : 8}>
                              <h3 className='account__title'>{MasterRecTitle}</h3>
                              <h4 className='account__subhead subhead'>{MasterRecSubtitle}</h4>
                            </Col>
                            <Col xs={useMobileView ? 3 : 4}>
                              <ButtonToolbar className='f-right'>
                                {(this.constructor.ShowSpinner(AdmRptElmState) && <Skeleton height='40px' />) ||
                                  <UncontrolledDropdown>
                                    <ButtonGroup className='btn-group--icons'>
                                      <i className={dirty ? 'fa fa-exclamation exclamation-icon' : ''}></i>
                                      {
                                        dropdownMenuButtonList.filter(v => !v.expose && !this.ActionSuppressed(authRow, v.buttonType, (currMst || {}).RptElmId160)).length > 0 &&
                                        <DropdownToggle className='mw-50' outline>
                                          <i className='fa fa-ellipsis-h icon-holder'></i>
                                          {!useMobileView && <p className='action-menu-label'>{(screenButtons.More || {}).label}</p>}
                                        </DropdownToggle>
                                      }
                                    </ButtonGroup>
                                    {
                                      dropdownMenuButtonList.filter(v => !v.expose).length > 0 &&
                                      <DropdownMenu right className={`dropdown__menu dropdown-options`}>
                                        {
                                          dropdownMenuButtonList.filter(v => !v.expose).map(v => {
                                            if (this.ActionSuppressed(authRow, v.buttonType, (currMst || {}).RptElmId160)) return null;
                                            return (
                                              <DropdownItem key={v.tid || v.order} onClick={this.ScreenButtonAction[v.buttonType]({ naviBar, submitForm, ScreenButton: v, mst: currMst, dtl: currDtl, useMobileView })} className={`${v.className}`}><i className={`${v.iconClassName} mr-10`}></i>{v.label}</DropdownItem>)
                                          })
                                        }
                                      </DropdownMenu>
                                    }
                                  </UncontrolledDropdown>
                                }
                              </ButtonToolbar>
                            </Col>
                          </Row>
                        </div>
                        <Form className='form'> {/* this line equals to <form className='form' onSubmit={handleSubmit} */}
                          {(selectedMst || {}).key ?
                            <div className='form__form-group'>
                              <div className='form__form-group-narrow'>
                                <div className='form__form-group-field'>
                                  <span className='radio-btn radio-btn--button btn--button-header h-20 no-pointer'>
                                    <span className='radio-btn__label color-blue fw-700 f-14'>{selectedMst.label || ''}</span>
                                    <span className='radio-btn__label__right color-blue fw-700 f-14'><span className='mr-5'>{selectedMst.labelR || ''}</span>
                                    </span>
                                  </span>
                                </div>
                              </div>
                              <div className='form__form-group-field'>
                                <span className='radio-btn radio-btn--button btn--button-header h-20 no-pointer'>
                                  <span className='radio-btn__label color-blue fw-700 f-14'>{selectedMst.detail || ''}</span>
                                  <span className='radio-btn__label__right color-blue fw-700 f-14'><span className='mr-5'>{selectedMst.detailR || ''}</span>
                                  </span>
                                </span>
                              </div>
                            </div>
                            :
                            <div className='form__form-group'>
                              <div className='form__form-group-narrow'>
                                <div className='form__form-group-field'>
                                  <span className='radio-btn radio-btn--button btn--button-header h-20 no-pointer'>
                                    <span className='radio-btn__label color-blue fw-700 f-14'>{NoMasterMsg}</span>
                                  </span>
                                </div>
                              </div>
                            </div>
                          }
                          <div className='w-100'>
                            <Row>
                              {(authCol.RptElmId160 || {}).visible &&
                                <Col lg={6} xl={6}>
                                  <div className='form__form-group'>
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='20px' />) ||
                                      <label className='form__form-group-label'>{(columnLabel.RptElmId160 || {}).ColumnHeader} {(columnLabel.RptElmId160 || {}).ToolTip &&
                                        (<ControlledPopover id={(columnLabel.RptElmId160 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.RptElmId160 || {}).ToolTip} />
                                        )}
                                      </label>
                                    }
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='36px' />) ||
                                      <div className='form__form-group-field'>
                                        <Field
                                          type='text'
                                          name='cRptElmId160'
                                          disabled={(authCol.RptElmId160 || {}).readonly ? 'disabled' : ''} />
                                      </div>
                                    }
                                    {errors.cRptElmId160 && touched.cRptElmId160 && <span className='form__form-group-error'>{errors.cRptElmId160}</span>}
                                  </div>
                                </Col>
                              }
                              {(authCol.ReportId160 || {}).visible &&
                                <Col lg={6} xl={6}>
                                  <div className='form__form-group'>
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='20px' />) ||
                                      <label className='form__form-group-label'>{(columnLabel.ReportId160 || {}).ColumnHeader} <span className='text-danger'>*</span>{(columnLabel.ReportId160 || {}).ToolTip &&
                                        (<ControlledPopover id={(columnLabel.ReportId160 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.ReportId160 || {}).ToolTip} />
                                        )}
                                      </label>
                                    }
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='36px' />) ||
                                      <div className='form__form-group-field'>
                                        <AutoCompleteField
                                          name='cReportId160'
                                          onChange={this.FieldChange(setFieldValue, setFieldTouched, 'cReportId160', false, values)}
                                          onBlur={this.FieldChange(setFieldValue, setFieldTouched, 'cReportId160', true)}
                                          onInputChange={this.ReportId160InputChange()}
                                          value={values.cReportId160}
                                          defaultSelected={ReportId160List.filter(obj => { return obj.key === ReportId160 })}
                                          options={ReportId160List}
                                          filterBy={this.AutoCompleteFilterBy}
                                          disabled={(authCol.ReportId160 || {}).readonly ? true : false} />
                                      </div>
                                    }
                                    {errors.cReportId160 && touched.cReportId160 && <span className='form__form-group-error'>{errors.cReportId160}</span>}
                                  </div>
                                </Col>
                              }
                              {(authCol.RptElmTypeCd160 || {}).visible &&
                                <Col lg={6} xl={6}>
                                  <div className='form__form-group'>
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='20px' />) ||
                                      <label className='form__form-group-label'>{(columnLabel.RptElmTypeCd160 || {}).ColumnHeader} <span className='text-danger'>*</span>{(columnLabel.RptElmTypeCd160 || {}).ToolTip &&
                                        (<ControlledPopover id={(columnLabel.RptElmTypeCd160 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.RptElmTypeCd160 || {}).ToolTip} />
                                        )}
                                      </label>
                                    }
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='36px' />) ||
                                      <div className='form__form-group-field'>
                                        <DropdownField
                                          name='cRptElmTypeCd160'
                                          onChange={this.DropdownChangeV1(setFieldValue, setFieldTouched, 'cRptElmTypeCd160')}
                                          value={values.cRptElmTypeCd160}
                                          options={RptElmTypeCd160List}
                                          placeholder=''
                                          disabled={(authCol.RptElmTypeCd160 || {}).readonly ? 'disabled' : ''} />
                                      </div>
                                    }
                                    {errors.cRptElmTypeCd160 && touched.cRptElmTypeCd160 && <span className='form__form-group-error'>{errors.cRptElmTypeCd160}</span>}
                                  </div>
                                </Col>
                              }
                              {(authCol.RptStyleId160 || {}).visible &&
                                <Col lg={6} xl={6}>
                                  <div className='form__form-group'>
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='20px' />) ||
                                      <label className='form__form-group-label'>{(columnLabel.RptStyleId160 || {}).ColumnHeader} {(columnLabel.RptStyleId160 || {}).ToolTip &&
                                        (<ControlledPopover id={(columnLabel.RptStyleId160 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.RptStyleId160 || {}).ToolTip} />
                                        )}
                                      </label>
                                    }
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='36px' />) ||
                                      <div className='form__form-group-field'>
                                        <AutoCompleteField
                                          name='cRptStyleId160'
                                          onChange={this.FieldChange(setFieldValue, setFieldTouched, 'cRptStyleId160', false, values)}
                                          onBlur={this.FieldChange(setFieldValue, setFieldTouched, 'cRptStyleId160', true)}
                                          onInputChange={this.RptStyleId160InputChange()}
                                          value={values.cRptStyleId160}
                                          defaultSelected={RptStyleId160List.filter(obj => { return obj.key === RptStyleId160 })}
                                          options={RptStyleId160List}
                                          filterBy={this.AutoCompleteFilterBy}
                                          disabled={(authCol.RptStyleId160 || {}).readonly ? true : false} />
                                      </div>
                                    }
                                    {errors.cRptStyleId160 && touched.cRptStyleId160 && <span className='form__form-group-error'>{errors.cRptStyleId160}</span>}
                                  </div>
                                </Col>
                              }
                              {(authCol.ElmHeight160 || {}).visible &&
                                <Col lg={6} xl={6}>
                                  <div className='form__form-group'>
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='20px' />) ||
                                      <label className='form__form-group-label'>{(columnLabel.ElmHeight160 || {}).ColumnHeader} <span className='text-danger'>*</span>{(columnLabel.ElmHeight160 || {}).ToolTip &&
                                        (<ControlledPopover id={(columnLabel.ElmHeight160 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.ElmHeight160 || {}).ToolTip} />
                                        )}
                                      </label>
                                    }
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='36px' />) ||
                                      <div className='form__form-group-field'>
                                        <Field
                                          type='text'
                                          name='cElmHeight160'
                                          disabled={(authCol.ElmHeight160 || {}).readonly ? 'disabled' : ''} />
                                      </div>
                                    }
                                    {errors.cElmHeight160 && touched.cElmHeight160 && <span className='form__form-group-error'>{errors.cElmHeight160}</span>}
                                  </div>
                                </Col>
                              }
                              {(authCol.ElmColumns160 || {}).visible &&
                                <Col lg={6} xl={6}>
                                  <div className='form__form-group'>
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='20px' />) ||
                                      <label className='form__form-group-label'>{(columnLabel.ElmColumns160 || {}).ColumnHeader} {(columnLabel.ElmColumns160 || {}).ToolTip &&
                                        (<ControlledPopover id={(columnLabel.ElmColumns160 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.ElmColumns160 || {}).ToolTip} />
                                        )}
                                      </label>
                                    }
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='36px' />) ||
                                      <div className='form__form-group-field'>
                                        <Field
                                          type='text'
                                          name='cElmColumns160'
                                          disabled={(authCol.ElmColumns160 || {}).readonly ? 'disabled' : ''} />
                                      </div>
                                    }
                                    {errors.cElmColumns160 && touched.cElmColumns160 && <span className='form__form-group-error'>{errors.cElmColumns160}</span>}
                                  </div>
                                </Col>
                              }
                              {(authCol.ElmColSpacing160 || {}).visible &&
                                <Col lg={6} xl={6}>
                                  <div className='form__form-group'>
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='20px' />) ||
                                      <label className='form__form-group-label'>{(columnLabel.ElmColSpacing160 || {}).ColumnHeader} {(columnLabel.ElmColSpacing160 || {}).ToolTip &&
                                        (<ControlledPopover id={(columnLabel.ElmColSpacing160 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.ElmColSpacing160 || {}).ToolTip} />
                                        )}
                                      </label>
                                    }
                                    {((true && this.constructor.ShowSpinner(AdmRptElmState)) && <Skeleton height='36px' />) ||
                                      <div className='form__form-group-field'>
                                        <Field
                                          type='text'
                                          name='cElmColSpacing160'
                                          disabled={(authCol.ElmColSpacing160 || {}).readonly ? 'disabled' : ''} />
                                      </div>
                                    }
                                    {errors.cElmColSpacing160 && touched.cElmColSpacing160 && <span className='form__form-group-error'>{errors.cElmColSpacing160}</span>}
                                  </div>
                                </Col>
                              }
                              {(authCol.ElmPrintFirst160 || {}).visible &&
                                <Col lg={12} xl={12}>
                                  <div className='form__form-group'>
                                    <label className='checkbox-btn checkbox-btn--colored-click'>
                                      <Field
                                        className='checkbox-btn__checkbox'
                                        type='checkbox'
                                        name='cElmPrintFirst160'
                                        onChange={handleChange}
                                        defaultChecked={values.cElmPrintFirst160}
                                        disabled={(authCol.ElmPrintFirst160 || {}).readonly || !(authCol.ElmPrintFirst160 || {}).visible}
                                      />
                                      <span className='checkbox-btn__checkbox-custom'><CheckIcon /></span>
                                      <span className='checkbox-btn__label'>{(columnLabel.ElmPrintFirst160 || {}).ColumnHeader}</span>
                                    </label>
                                    {(columnLabel.ElmPrintFirst160 || {}).ToolTip &&
                                      (<ControlledPopover id={(columnLabel.ElmPrintFirst160 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.ElmPrintFirst160 || {}).ToolTip} />
                                      )}
                                  </div>
                                </Col>
                              }
                              {(authCol.ElmPrintLast160 || {}).visible &&
                                <Col lg={12} xl={12}>
                                  <div className='form__form-group'>
                                    <label className='checkbox-btn checkbox-btn--colored-click'>
                                      <Field
                                        className='checkbox-btn__checkbox'
                                        type='checkbox'
                                        name='cElmPrintLast160'
                                        onChange={handleChange}
                                        defaultChecked={values.cElmPrintLast160}
                                        disabled={(authCol.ElmPrintLast160 || {}).readonly || !(authCol.ElmPrintLast160 || {}).visible}
                                      />
                                      <span className='checkbox-btn__checkbox-custom'><CheckIcon /></span>
                                      <span className='checkbox-btn__label'>{(columnLabel.ElmPrintLast160 || {}).ColumnHeader}</span>
                                    </label>
                                    {(columnLabel.ElmPrintLast160 || {}).ToolTip &&
                                      (<ControlledPopover id={(columnLabel.ElmPrintLast160 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.ElmPrintLast160 || {}).ToolTip} />
                                      )}
                                  </div>
                                </Col>
                              }
                            </Row>
                          </div>
                          <div className='form__form-group mart-5 mb-0'>
                            <Row className='btn-bottom-row'>
                              {useMobileView && <Col xs={3} sm={2} className='btn-bottom-column'>
                                <Button color='success' className='btn btn-outline-success account__btn' onClick={this.props.history.goBack} outline><i className='fa fa-long-arrow-left'></i></Button>
                              </Col>}
                              <Col
                                xs={useMobileView ? 9 : 12}
                                sm={useMobileView ? 10 : 12}>
                                <Row>
                                  {
                                    bottomButtonList
                                      .filter(v => v.expose)
                                      .map((v, i, a) => {
                                        if (this.ActionSuppressed(authRow, v.buttonType, (currMst || {}).RptElmId160)) return null;
                                        const buttonCount = a.length - a.filter(x => this.ActionSuppressed(authRow, x.buttonType, (currMst || {}).RptElmId160));
                                        const colWidth = parseInt(12 / buttonCount, 10);
                                        const lastBtn = i === (a.length - 1);
                                        const outlineProperty = lastBtn ? false : true;
                                        return (
                                          <Col key={v.tid || v.order} xs={colWidth} sm={colWidth} className='btn-bottom-column' >
                                            {(this.constructor.ShowSpinner(AdmRptElmState) && <Skeleton height='43px' />) ||
                                              <Button color='success' type='button' outline={outlineProperty} className='account__btn' disabled={isSubmitting} onClick={this.ScreenButtonAction[v.buttonType]({ naviBar, submitForm, ScreenButton: v, mst: currMst, useMobileView })}>{v.label}</Button>
                                            }
                                          </Col>
                                        )
                                      })
                                  }
                                </Row>
                              </Col>
                            </Row>
                          </div>
                        </Form>
                      </div>
                    )}
                />
              </div>
            </div>
          </div>
        </div>
      </DocumentTitle>
    );
  };
};

const mapStateToProps = (state) => ({
  user: (state.auth || {}).user,
  error: state.error,
  AdmRptElm: state.AdmRptElm,
  global: state.global,
});

const mapDispatchToProps = (dispatch) => (
  bindActionCreators(Object.assign({},
    { LoadPage: AdmRptElmReduxObj.LoadPage.bind(AdmRptElmReduxObj) },
    { SavePage: AdmRptElmReduxObj.SavePage.bind(AdmRptElmReduxObj) },
    { DelMst: AdmRptElmReduxObj.DelMst.bind(AdmRptElmReduxObj) },
    { AddMst: AdmRptElmReduxObj.AddMst.bind(AdmRptElmReduxObj) },
    { SearchReportId160: AdmRptElmReduxObj.SearchActions.SearchReportId160.bind(AdmRptElmReduxObj) },
    { SearchRptStyleId160: AdmRptElmReduxObj.SearchActions.SearchRptStyleId160.bind(AdmRptElmReduxObj) },
    { showNotification: showNotification },
    { setTitle: setTitle },
    { setSpinner: setSpinner },
  ), dispatch)
)

export default connect(mapStateToProps, mapDispatchToProps)(MstRecord);
