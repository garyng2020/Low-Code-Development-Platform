
import { fetchData, getAccessControlInfo, getAccessScope, baseUrl } from './webAPIBase';

let activeScope = {};

export function setAccessScope(scope) {
    activeScope = {
        ...activeScope,
        ...scope,
    }
}

export function GetAuthCol(accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetAuthCol'
        , {
            requestOptions: {
                body: JSON.stringify({
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetAuthRow(accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetAuthRow'
        , {
            requestOptions: {
                body: JSON.stringify({
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )

}
export function GetScreenLabel(accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetScreenLabel'
        , {
            requestOptions: {
                body: JSON.stringify({
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope || getAccessScope())
        }
    )

}

export function GetLabels(labelCat, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetLabels'
        , {
            requestOptions: {
                body: JSON.stringify({
                    labelCat: labelCat
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}

export function GetSystemLabels(labelCat, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetSystemLabels'
        , {
            requestOptions: {
                body: JSON.stringify({
                    labelCat: labelCat
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}

export function GetScreenButtonHlp(labelCat, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetScreenButtonHlp'
        , {
            requestOptions: {
                body: JSON.stringify({
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetScreenHlp(labelCat, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetScreenHlp'
        , {
            requestOptions: {
                body: JSON.stringify({
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetScreenCriteria(accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetScreenCriteria'
        , {
            requestOptions: {
                body: JSON.stringify({
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetNewMst(accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetNewMst'
        , {
            requestOptions: {
                body: JSON.stringify({
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetNewDtl(accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetNewDtl'
        , {
            requestOptions: {
                body: JSON.stringify({
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetScreenFilter(accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetScreenFilter'
        , {
            requestOptions: {
                body: JSON.stringify({
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetSearchList(searchStr, topN, filterId, desiredScreenCriteria, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetSearchList'
        , {
            requestOptions: {
                body: JSON.stringify({
                    searchStr: searchStr || '',
                    topN: topN || 0,
                    filterId: ('' + (filterId || 0)),
                    desiredScreenCriteria: desiredScreenCriteria || {},
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetAdmMember122List(searchStr, topN, filterId, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetAdmMember122List'
        , {
            requestOptions: {
                body: JSON.stringify({
                    searchStr: searchStr || '',
                    topN: topN || 0,
                    filterId: ('' + (filterId || 0)),
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetAdmMember122ById(keyId, options, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetAdmMember122ById'
        , {
            requestOptions: {
                body: JSON.stringify({
                    keyId: keyId || '',
                    options: options || {
                        CurrentScreenCriteria: JSON.stringify({}),
                    },
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export const GetMstById = GetAdmMember122ById;
export function GetAdmMember122DtlById(keyId, filterId, options, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetAdmMember122DtlById'
        , {
            requestOptions: {
                body: JSON.stringify({
                    keyId: keyId || '',
                    options: options || {
                        CurrentScreenCriteria: JSON.stringify({}),
                    },
                    filterId: filterId || 0,
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export const GetDtlById = GetAdmMember122DtlById;
export function LoadInitPage(options, accessScope) {
    const reqJson = JSON.stringify({
        options: options
    });
    return fetchData(baseUrl + '/AdmMemberWs.asmx/LoadInitPage'
        , {
            requestOptions: {
                body: reqJson,
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function SaveData(mst, dtl, options, accessScope) {
    const reqJson = JSON.stringify({
        mst: mst || {},
        dtl: dtl || [],
        options: options || {}
    });
    return fetchData(baseUrl + '/AdmMemberWs.asmx/SaveData'
        , {
            requestOptions: {
                body: reqJson,
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function DelMst(mst, options, accessScope) {
    const reqJson = JSON.stringify({
        mst: mst,
        options: options
    });
    return fetchData(baseUrl + '/AdmMemberWs.asmx/DelMst'
        , {
            requestOptions: {
                body: reqJson,
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function SetScreenCriteria(criteriaValues, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/SetScreenCriteria'
        , {
            requestOptions: {
                body: JSON.stringify({
                    criteriaValues: criteriaValues
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}
export function GetRefColumnContent(mstId, dtlId, refKeyId, isMaster, refScreenColumnName, options, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetRefColumnContent'
        , {
            requestOptions: {
                body: JSON.stringify({
                    mstId: mstId || null,
                    dtlId: dtlId || null,
                    refKeyId: refKeyId || null,
                    refScreenColumnName: refScreenColumnName || null,
                    isMaster: isMaster || false,
                    options: options || {},
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}

export function GetColumnContent(mstId, dtlId, columnName, isMaster, screenColumnName, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetColumnContent'
        , {
            requestOptions: {
                body: JSON.stringify({
                    mstId: mstId || '',
                    dtlId: dtlId || '',
                    screenColumnName: screenColumnName,
                    columnName: columnName,
                    isMaster: isMaster || false,
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}

export function GetEmbeddedDoc(mstId, dtlId, isMaster, screenColumnName, accessScope) {
    const reqJson = JSON.stringify({
        mstId: mstId || '',
        dtlId: dtlId || '',
        isMaster: isMaster || false,
        columnName: screenColumnName || '',
        screenColumnName: screenColumnName || '',
    });
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetColumnContent'
        , {
            requestOptions: {
                body: reqJson,
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}

export function SaveEmbeddedImage(mstId, dtlId, isMaster, screenColumnName, docJson, options, accessScope) {
    const reqJson = JSON.stringify({
        mstId: mstId || '',
        dtlId: dtlId || '',
        isMaster: isMaster || false,
        screenColumnName: screenColumnName || '',
        docJson: docJson || '',
        options: options || {},
    });
    return fetchData(baseUrl + '/AdmMemberWs.asmx/AddDocColumnContent'
        , {
            requestOptions: {
                body: reqJson,
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}

export function GetDocZipDownload(keyId, options, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetDocZipDownload'
        , {
            requestOptions: {
                body: JSON.stringify({
                    keyId: keyId || null,
                    options: options ||{},
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}

/*screen criteria dll and screen dropdownlist/autocomplete*/

export function GetUserId274List(query, topN, filterBy, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetUserId274List'
        , {
            requestOptions: {
                body: JSON.stringify({
                    query: query || '',
                    topN: topN || 0,
                    filterBy: filterBy || null
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}


export function GetMemberId274List(query, topN, filterBy, accessScope) {
    return fetchData(baseUrl + '/AdmMemberWs.asmx/GetMemberId274List'
        , {
            requestOptions: {
                body: JSON.stringify({
                    query: query || '',
                    topN: topN || 0,
                    filterBy: filterBy || null
                }),
            },
            ...(getAccessControlInfo()),
            ...(accessScope)
        }
    )
}

/* ReactRule: Service Custom Function */


/* ReactRule End: Service Custom Function */
