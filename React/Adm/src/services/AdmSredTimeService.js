
import { fetchData, getAccessControlInfo, getAccessScope, baseUrl } from './webAPIBase';

let activeScope = {};

export function setAccessScope(scope) {
    activeScope = {
        ...activeScope,
        ...scope,
    }
}

export function GetAuthCol(accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetAuthCol'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetAuthRow'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetScreenLabel'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetLabels'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetSystemLabels'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetScreenButtonHlp'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetScreenHlp'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetScreenCriteria'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetNewMst'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetNewDtl'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetScreenFilter'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetSearchList'
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
export function GetAdmSredTime123List(searchStr, topN, filterId, accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetAdmSredTime123List'
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
export function GetAdmSredTime123ById(keyId, options, accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetAdmSredTime123ById'
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
export const GetMstById = GetAdmSredTime123ById;
export function GetAdmSredTime123DtlById(keyId, filterId, options, accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetAdmSredTime123DtlById'
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
export const GetDtlById = GetAdmSredTime123DtlById;
export function LoadInitPage(options, accessScope) {
    const reqJson = JSON.stringify({
        options: options
    });
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/LoadInitPage'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/SaveData'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/DelMst'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/SetScreenCriteria'
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
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetRefColumnContent'
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

/*screen criteria dll and screen dropdownlist/autocomplete*/

export function GetScreenCriMemberId10List(query, topN, filterBy, accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetScreenCriteriaDdlList'
        , {
            requestOptions: {
                body: JSON.stringify({
                    screenCriId: 63,
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


export function GetScreenCriSredTimeDt20List(query, topN, filterBy, accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetScreenCriteriaDdlList'
        , {
            requestOptions: {
                body: JSON.stringify({
                    screenCriId: 65,
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


export function GetScreenCriSredTimeDt30List(query, topN, filterBy, accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetScreenCriteriaDdlList'
        , {
            requestOptions: {
                body: JSON.stringify({
                    screenCriId: 66,
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

export function GetMemberId272List(query, topN, filterBy, accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetMemberId272List'
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


export function GetEnteredBy272List(query, topN, filterBy, accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetEnteredBy272List'
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


export function GetModifiedBy272List(query, topN, filterBy, accessScope) {
    return fetchData(baseUrl + '/AdmSredTimeWs.asmx/GetModifiedBy272List'
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
