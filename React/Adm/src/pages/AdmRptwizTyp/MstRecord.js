
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
import AdmRptwizTypReduxObj, { ShowMstFilterApplied } from '../../redux/AdmRptwizTyp';
import * as AdmRptwizTypService from '../../services/AdmRptwizTypService';
import { getRintagiConfig } from '../../helpers/config';
import Skeleton from 'react-skeleton-loader';
import ControlledPopover from '../../components/custom/ControlledPopover';
import log from '../../helpers/logger';

class MstRecord extends RintagiScreen {
  constructor(props) {
    super(props);
    this.GetReduxState = () => (this.props.AdmRptwizTyp || {});
    this.blocker = null;
    this.titleSet = false;
    this.MstKeyColumnName = 'RptwizTypId185';
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


  /* ReactRule: Master Record Custom Function */

  /* ReactRule End: Master Record Custom Function */

  /* form related input handling */

  ValidatePage(values) {
    const errors = {};
    const columnLabel = (this.props.AdmRptwizTyp || {}).ColumnLabel || {};
    /* standard field validation */
    if (!values.cRptwizTypName185) { errors.cRptwizTypName185 = (columnLabel.RptwizTypName185 || {}).ErrMessage; }
    return errors;
  }

  SavePage(values, { setSubmitting, setErrors, resetForm, setFieldValue, setValues }) {
    const errors = [];
    const currMst = (this.props.AdmRptwizTyp || {}).Mst || {};

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
        this.props.AdmRptwizTyp,
        {
          RptwizTypId185: values.cRptwizTypId185 || '',
          RptwizTypName185: values.cRptwizTypName185 || '',
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
    const AdmRptwizTypState = this.props.AdmRptwizTyp || {};
    const auxSystemLabels = AdmRptwizTypState.SystemLabel || {};
    return function (evt) {
      evt.preventDefault();
      const fromMstId = mstId || (mst || {}).RptwizTypId185;
      const copyFn = () => {
        if (fromMstId) {
          this.props.AddMst(fromMstId, 'MstRecord', 0);
          /* this is application specific rule as the Posted flag needs to be reset */
          this.props.AdmRptwizTyp.Mst.Posted64 = 'N';
          if (useMobileView) {
            const naviBar = getNaviBar('MstRecord', {}, {}, this.props.AdmRptwizTyp.Label);
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
    const AdmRptwizTypState = this.props.AdmRptwizTyp || {};
    const auxSystemLabels = AdmRptwizTypState.SystemLabel || {};
    return function (evt) {
      evt.preventDefault();
      const deleteFn = () => {
        const fromMstId = mstId || mst.RptwizTypId185;
        this.props.DelMst(this.props.AdmRptwizTyp, fromMstId);
      };
      this.setState({ ModalOpen: true, ModalSuccess: deleteFn, ModalColor: 'danger', ModalTitle: auxSystemLabels.WarningTitle || '', ModalMsg: auxSystemLabels.DeletePageMsg || '' });
    }.bind(this);
  }
  /* end of screen button action */

  /* react related stuff */
  static getDerivedStateFromProps(nextProps, prevState) {
    const nextReduxScreenState = nextProps.AdmRptwizTyp || {};
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
    const AdmRptwizTypState = this.props.AdmRptwizTyp || {};
    const auxSystemLabels = AdmRptwizTypState.SystemLabel || {};
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
      if (!(this.props.AdmRptwizTyp || {}).AuthCol || true) {
        this.props.LoadPage('MstRecord', { mstId: mstId || '_' });
      }
    }
    else {
      return;
    }
  }

  componentDidUpdate(prevprops, prevstates) {
    const currReduxScreenState = this.props.AdmRptwizTyp || {};

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
    const AdmRptwizTypState = this.props.AdmRptwizTyp || {};

    if (AdmRptwizTypState.access_denied) {
      return <Redirect to='/error' />;
    }

    const screenHlp = AdmRptwizTypState.ScreenHlp;

    // Labels
    const siteTitle = (this.props.global || {}).pageTitle || '';
    const MasterRecTitle = ((screenHlp || {}).MasterRecTitle || '');
    const MasterRecSubtitle = ((screenHlp || {}).MasterRecSubtitle || '');
    const NoMasterMsg = ((screenHlp || {}).NoMasterMsg || '');

    const screenButtons = AdmRptwizTypReduxObj.GetScreenButtons(AdmRptwizTypState) || {};
    const itemList = AdmRptwizTypState.Dtl || [];
    const auxLabels = AdmRptwizTypState.Label || {};
    const auxSystemLabels = AdmRptwizTypState.SystemLabel || {};

    const columnLabel = AdmRptwizTypState.ColumnLabel || {};
    const authCol = this.GetAuthCol(AdmRptwizTypState);
    const authRow = (AdmRptwizTypState.AuthRow || [])[0] || {};
    const currMst = ((this.props.AdmRptwizTyp || {}).Mst || {});
    const currDtl = ((this.props.AdmRptwizTyp || {}).EditDtl || {});
    const naviBar = getNaviBar('MstRecord', currMst, currDtl, screenButtons).filter(v => ((v.type !== 'DtlRecord' && v.type !== 'DtlList') || currMst.RptwizTypId185));
    const selectList = AdmRptwizTypReduxObj.SearchListToSelectList(AdmRptwizTypState);
    const selectedMst = (selectList || []).filter(v => v.isSelected)[0] || {};

    const RptwizTypId185 = currMst.RptwizTypId185;
    const RptwizTypName185 = currMst.RptwizTypName185;

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
                {/* {!useMobileView && this.constructor.ShowSpinner(AdmRptwizTypState) && <div className='panel__refresh'></div>} */}
                {useMobileView && <div className='tabs tabs--justify tabs--bordered-bottom'>
                  <div className='tabs__wrap'>
                    <NaviBar history={this.props.history} navi={naviBar} />
                  </div>
                </div>}
                <p className='project-title-mobile mb-10'>{siteTitle.substring(0, document.title.indexOf('-') - 1)}</p>
                <Formik
                  initialValues={{
                    cRptwizTypId185: formatContent(RptwizTypId185 || '', 'TextBox'),
                    cRptwizTypName185: formatContent(RptwizTypName185 || '', 'TextBox'),
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
                                {(this.constructor.ShowSpinner(AdmRptwizTypState) && <Skeleton height='40px' />) ||
                                  <UncontrolledDropdown>
                                    <ButtonGroup className='btn-group--icons'>
                                      <i className={dirty ? 'fa fa-exclamation exclamation-icon' : ''}></i>
                                      {
                                        dropdownMenuButtonList.filter(v => !v.expose && !this.ActionSuppressed(authRow, v.buttonType, (currMst || {}).RptwizTypId185)).length > 0 &&
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
                                            if (this.ActionSuppressed(authRow, v.buttonType, (currMst || {}).RptwizTypId185)) return null;
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
                              {(authCol.RptwizTypId185 || {}).visible &&
                                <Col lg={6} xl={6}>
                                  <div className='form__form-group'>
                                    {((true && this.constructor.ShowSpinner(AdmRptwizTypState)) && <Skeleton height='20px' />) ||
                                      <label className='form__form-group-label'>{(columnLabel.RptwizTypId185 || {}).ColumnHeader} {(columnLabel.RptwizTypId185 || {}).ToolTip &&
                                        (<ControlledPopover id={(columnLabel.RptwizTypId185 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.RptwizTypId185 || {}).ToolTip} />
                                        )}
                                      </label>
                                    }
                                    {((true && this.constructor.ShowSpinner(AdmRptwizTypState)) && <Skeleton height='36px' />) ||
                                      <div className='form__form-group-field'>
                                        <Field
                                          type='text'
                                          name='cRptwizTypId185'
                                          disabled={(authCol.RptwizTypId185 || {}).readonly ? 'disabled' : ''} />
                                      </div>
                                    }
                                    {errors.cRptwizTypId185 && touched.cRptwizTypId185 && <span className='form__form-group-error'>{errors.cRptwizTypId185}</span>}
                                  </div>
                                </Col>
                              }
                              {(authCol.RptwizTypName185 || {}).visible &&
                                <Col lg={6} xl={6}>
                                  <div className='form__form-group'>
                                    {((true && this.constructor.ShowSpinner(AdmRptwizTypState)) && <Skeleton height='20px' />) ||
                                      <label className='form__form-group-label'>{(columnLabel.RptwizTypName185 || {}).ColumnHeader} <span className='text-danger'>*</span>{(columnLabel.RptwizTypName185 || {}).ToolTip &&
                                        (<ControlledPopover id={(columnLabel.RptwizTypName185 || {}).ColumnName} className='sticky-icon pt-0 lh-23' message={(columnLabel.RptwizTypName185 || {}).ToolTip} />
                                        )}
                                      </label>
                                    }
                                    {((true && this.constructor.ShowSpinner(AdmRptwizTypState)) && <Skeleton height='36px' />) ||
                                      <div className='form__form-group-field'>
                                        <Field
                                          type='text'
                                          name='cRptwizTypName185'
                                          disabled={(authCol.RptwizTypName185 || {}).readonly ? 'disabled' : ''} />
                                      </div>
                                    }
                                    {errors.cRptwizTypName185 && touched.cRptwizTypName185 && <span className='form__form-group-error'>{errors.cRptwizTypName185}</span>}
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
                                        if (this.ActionSuppressed(authRow, v.buttonType, (currMst || {}).RptwizTypId185)) return null;
                                        const buttonCount = a.length - a.filter(x => this.ActionSuppressed(authRow, x.buttonType, (currMst || {}).RptwizTypId185));
                                        const colWidth = parseInt(12 / buttonCount, 10);
                                        const lastBtn = i === (a.length - 1);
                                        const outlineProperty = lastBtn ? false : true;
                                        return (
                                          <Col key={v.tid || v.order} xs={colWidth} sm={colWidth} className='btn-bottom-column' >
                                            {(this.constructor.ShowSpinner(AdmRptwizTypState) && <Skeleton height='43px' />) ||
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
  AdmRptwizTyp: state.AdmRptwizTyp,
  global: state.global,
});

const mapDispatchToProps = (dispatch) => (
  bindActionCreators(Object.assign({},
    { LoadPage: AdmRptwizTypReduxObj.LoadPage.bind(AdmRptwizTypReduxObj) },
    { SavePage: AdmRptwizTypReduxObj.SavePage.bind(AdmRptwizTypReduxObj) },
    { DelMst: AdmRptwizTypReduxObj.DelMst.bind(AdmRptwizTypReduxObj) },
    { AddMst: AdmRptwizTypReduxObj.AddMst.bind(AdmRptwizTypReduxObj) },

    { showNotification: showNotification },
    { setTitle: setTitle },
    { setSpinner: setSpinner },
  ), dispatch)
)

export default connect(mapStateToProps, mapDispatchToProps)(MstRecord);
