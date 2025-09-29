import { Loader, LoaderOptions } from '@googlemaps/js-api-loader';

type LocationInfo = {
    address: string;
    lat: number;
    lng: number;
};

const pmcAddress = {
    PMC: { address: '407台灣台中市西屯區工業區三十七路27號', lat: 24.17223, lng: 120.58912 },
    CIIC: { address: '600台灣嘉義市西區博愛路二段569號', lat: 23.467662, lng: 120.427864 },
    TPE: { address: '106台灣台北市大安區信義路三段41號', lat: 25.03375, lng: 121.535 }
};

// 定義返回結果的類型
export interface GMapResult {
    distanceKm: number;
    startAddress: string;
    endAddress: string;
    mapLink: string;
}

// 動態加載 Google Maps API 的函數
export async function loadGoogleMapsApi(apiKey: string): Promise<typeof google> {
    try {
        const loaderOptions: LoaderOptions = {
            apiKey,
            version: 'weekly',
            language: 'zh-TW'
            // libraries: ["places"], // 根據需要添加其他庫
        };

        const loader = new Loader(loaderOptions);
        await loader.importLibrary('maps');
        return window.google;
    } catch (error) {
        throw new Error(`Google Maps API 加載失敗: ${error}`);
    }
}

// 初始化 Google Maps API 的路徑計算方法
export async function calculateDistance(departure: string, destinationAddress: string): Promise<GMapResult> {
    try {
        console.log('destinationAddress:', destinationAddress);
        return await new Promise((resolve, reject) => {
            const departureInfo = pmcAddress[departure as keyof typeof pmcAddress] || pmcAddress.PMC;

            const request = {
                origin: departureInfo.address,
                destination: destinationAddress,
                optimizeWaypoints: true,
                travelMode: google.maps.TravelMode.DRIVING
            };

            const directionsService = new google.maps.DirectionsService();

            directionsService.route(request, (response, status) => {
                try {
                    if (status === google.maps.DirectionsStatus.OK) {
                        if (!response || !response.routes || response.routes.length === 0) {
                            reject(new Error('無法找到路徑，請確認輸入正確的地址！'));
                            return;
                        }
                        const route = response.routes[0];
                        const distanceKm = route.legs[0].distance ? Math.ceil(route.legs[0].distance.value / 1000) * 2 : 0;
                        const quotient = Math.floor(distanceKm / 5);
                        const finalDistance = distanceKm % 5 > 0 ? (quotient + 1) * 5 : quotient * 5; // 以 5 公里為單位計算

                        const startAddress = route.legs[0].start_address;
                        const endAddress = route.legs[0].end_address;
                        const mapLink = `https://maps.google.com/maps?saddr=${encodeURIComponent(startAddress)}&daddr=${encodeURIComponent(endAddress)}&hl=zh-TW&t=m`;

                        resolve({
                            distanceKm: finalDistance,
                            startAddress,
                            endAddress,
                            mapLink
                        });
                    } else if (status === google.maps.DirectionsStatus.INVALID_REQUEST) {
                        reject('請求無效，請確認輸入的地址正確。');
                    } else if (status === google.maps.DirectionsStatus.MAX_WAYPOINTS_EXCEEDED) {
                        reject('路徑點數量超出上限，請減少路徑點數量。');
                    } else if (status === google.maps.DirectionsStatus.NOT_FOUND) {
                        reject('找不到有效地址，請確認地址是否輸入正確。');
                    } else if (status === google.maps.DirectionsStatus.ZERO_RESULTS) {
                        reject('無法找到路徑，請確認輸入正確的地址！');
                    } else if (status === google.maps.DirectionsStatus.OVER_QUERY_LIMIT) {
                        reject('查詢次數過多，請稍候再試或通知資訊人員');
                    } else if (status === google.maps.DirectionsStatus.REQUEST_DENIED) {
                        reject('請求被拒絕，可能是 API 金鑰無效或權限不足。');
                    } else if (status === google.maps.DirectionsStatus.UNKNOWN_ERROR) {
                        reject('未知錯誤，請稍後再試。');
                    } else {
                        reject(`GoogleMaps 未正確回應，錯誤狀態: ${status}`);
                    }
                } catch (error) {
                    reject(`處理路徑計算結果時發生錯誤: ${error}`);
                }
            });
        });
    } catch (error) {
        throw new Error(`計算距離失敗: ${error}`);
    }
}
