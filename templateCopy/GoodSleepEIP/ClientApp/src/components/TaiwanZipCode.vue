<!-- 
 vue3/primevue-TaiwanZipCode 台灣郵遞區號選擇，版權所有: kenghua@gmail.com 
  -->
<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import Select from 'primevue/select';
import InputText from 'primevue/inputtext';

interface City {
    name: string;
    code: string;
    districts: District[];
}

interface District {
    name: string;
    zipCode: string;
}

const cities: City[] = [
    {
        name: '台北市',
        code: 'TPE',
        districts: [
            { name: '中正區', zipCode: '100' },
            { name: '大同區', zipCode: '103' },
            { name: '中山區', zipCode: '104' },
            { name: '松山區', zipCode: '105' },
            { name: '大安區', zipCode: '106' },
            { name: '萬華區', zipCode: '108' },
            { name: '信義區', zipCode: '110' },
            { name: '士林區', zipCode: '111' },
            { name: '北投區', zipCode: '112' },
            { name: '內湖區', zipCode: '114' },
            { name: '南港區', zipCode: '115' },
            { name: '文山區', zipCode: '116' }
        ]
    },
    {
        name: '新北市',
        code: 'NTP',
        districts: [
            { name: '板橋區', zipCode: '220' },
            { name: '新莊區', zipCode: '242' },
            { name: '中和區', zipCode: '235' },
            { name: '永和區', zipCode: '234' },
            { name: '土城區', zipCode: '236' },
            { name: '樹林區', zipCode: '238' },
            { name: '三峽區', zipCode: '237' },
            { name: '鶯歌區', zipCode: '239' },
            { name: '三重區', zipCode: '241' },
            { name: '蘆洲區', zipCode: '247' },
            { name: '五股區', zipCode: '248' },
            { name: '泰山區', zipCode: '243' },
            { name: '林口區', zipCode: '244' },
            { name: '八里區', zipCode: '249' },
            { name: '淡水區', zipCode: '251' },
            { name: '三芝區', zipCode: '252' },
            { name: '石門區', zipCode: '253' },
            { name: '金山區', zipCode: '208' },
            { name: '萬里區', zipCode: '207' },
            { name: '汐止區', zipCode: '221' },
            { name: '瑞芳區', zipCode: '224' },
            { name: '貢寮區', zipCode: '228' },
            { name: '平溪區', zipCode: '226' },
            { name: '雙溪區', zipCode: '227' },
            { name: '新店區', zipCode: '231' },
            { name: '深坑區', zipCode: '222' },
            { name: '石碇區', zipCode: '223' },
            { name: '坪林區', zipCode: '232' },
            { name: '烏來區', zipCode: '233' }
        ]
    },
    {
        name: '基隆市',
        code: 'KEE',
        districts: [
            { name: '仁愛區', zipCode: '200' },
            { name: '信義區', zipCode: '201' },
            { name: '中正區', zipCode: '202' },
            { name: '中山區', zipCode: '203' },
            { name: '安樂區', zipCode: '204' },
            { name: '暖暖區', zipCode: '205' },
            { name: '七堵區', zipCode: '206' }
        ]
    },
    {
        name: '桃園市',
        code: 'TYN',
        districts: [
            { name: '桃園區', zipCode: '330' },
            { name: '中壢區', zipCode: '320' },
            { name: '平鎮區', zipCode: '324' },
            { name: '八德區', zipCode: '334' },
            { name: '楊梅區', zipCode: '326' },
            { name: '蘆竹區', zipCode: '338' },
            { name: '大溪區', zipCode: '335' },
            { name: '龜山區', zipCode: '333' },
            { name: '大園區', zipCode: '337' },
            { name: '觀音區', zipCode: '328' },
            { name: '新屋區', zipCode: '327' },
            { name: '龍潭區', zipCode: '325' },
            { name: '復興區', zipCode: '336' }
        ]
    },
    {
        name: '新竹市',
        code: 'HSZ',
        districts: [
            { name: '東區', zipCode: '300' },
            { name: '北區', zipCode: '300' },
            { name: '香山區', zipCode: '300' }
        ]
    },
    {
        name: '新竹縣',
        code: 'HSQ',
        districts: [
            { name: '竹北市', zipCode: '302' },
            { name: '竹東鎮', zipCode: '310' },
            { name: '新埔鎮', zipCode: '305' },
            { name: '關西鎮', zipCode: '306' },
            { name: '湖口鄉', zipCode: '303' },
            { name: '新豐鄉', zipCode: '304' },
            { name: '峨眉鄉', zipCode: '315' },
            { name: '寶山鄉', zipCode: '308' },
            { name: '北埔鄉', zipCode: '314' },
            { name: '芎林鄉', zipCode: '307' },
            { name: '橫山鄉', zipCode: '312' },
            { name: '尖石鄉', zipCode: '313' },
            { name: '五峰鄉', zipCode: '311' }
        ]
    },
    {
        name: '苗栗縣',
        code: 'MIA',
        districts: [
            { name: '苗栗市', zipCode: '360' },
            { name: '頭份市', zipCode: '351' },
            { name: '竹南鎮', zipCode: '350' },
            { name: '後龍鎮', zipCode: '356' },
            { name: '通霄鎮', zipCode: '357' },
            { name: '苑裡鎮', zipCode: '358' },
            { name: '卓蘭鎮', zipCode: '369' },
            { name: '造橋鄉', zipCode: '361' },
            { name: '西湖鄉', zipCode: '368' },
            { name: '頭屋鄉', zipCode: '362' },
            { name: '公館鄉', zipCode: '363' },
            { name: '銅鑼鄉', zipCode: '366' },
            { name: '三義鄉', zipCode: '367' },
            { name: '大湖鄉', zipCode: '364' },
            { name: '獅潭鄉', zipCode: '354' },
            { name: '三灣鄉', zipCode: '352' },
            { name: '南庄鄉', zipCode: '353' },
            { name: '泰安鄉', zipCode: '365' }
        ]
    },
    {
        name: '台中市',
        code: 'TXG',
        districts: [
            { name: '中區', zipCode: '400' },
            { name: '東區', zipCode: '401' },
            { name: '南區', zipCode: '402' },
            { name: '西區', zipCode: '403' },
            { name: '北區', zipCode: '404' },
            { name: '北屯區', zipCode: '406' },
            { name: '西屯區', zipCode: '407' },
            { name: '南屯區', zipCode: '408' },
            { name: '太平區', zipCode: '411' },
            { name: '大里區', zipCode: '412' },
            { name: '霧峰區', zipCode: '413' },
            { name: '烏日區', zipCode: '414' },
            { name: '豐原區', zipCode: '420' },
            { name: '后里區', zipCode: '421' },
            { name: '石岡區', zipCode: '422' },
            { name: '東勢區', zipCode: '423' },
            { name: '和平區', zipCode: '424' },
            { name: '新社區', zipCode: '426' },
            { name: '潭子區', zipCode: '427' },
            { name: '大雅區', zipCode: '428' },
            { name: '神岡區', zipCode: '429' },
            { name: '大肚區', zipCode: '432' },
            { name: '沙鹿區', zipCode: '433' },
            { name: '龍井區', zipCode: '434' },
            { name: '梧棲區', zipCode: '435' },
            { name: '清水區', zipCode: '436' },
            { name: '大甲區', zipCode: '437' },
            { name: '外埔區', zipCode: '438' },
            { name: '大安區', zipCode: '439' }
        ]
    },
    {
        name: '彰化縣',
        code: 'CWH',
        districts: [
            { name: '彰化市', zipCode: '500' },
            { name: '員林市', zipCode: '510' },
            { name: '和美鎮', zipCode: '508' },
            { name: '鹿港鎮', zipCode: '505' },
            { name: '溪湖鎮', zipCode: '514' },
            { name: '二林鎮', zipCode: '526' },
            { name: '田中鎮', zipCode: '520' },
            { name: '北斗鎮', zipCode: '521' },
            { name: '花壇鄉', zipCode: '503' },
            { name: '芬園鄉', zipCode: '502' },
            { name: '大村鄉', zipCode: '515' },
            { name: '永靖鄉', zipCode: '512' },
            { name: '伸港鄉', zipCode: '509' },
            { name: '線西鄉', zipCode: '507' },
            { name: '福興鄉', zipCode: '506' },
            { name: '秀水鄉', zipCode: '504' },
            { name: '埔心鄉', zipCode: '513' },
            { name: '埔鹽鄉', zipCode: '516' },
            { name: '大城鄉', zipCode: '527' },
            { name: '芳苑鄉', zipCode: '528' },
            { name: '竹塘鄉', zipCode: '525' },
            { name: '社頭鄉', zipCode: '511' },
            { name: '二水鄉', zipCode: '530' },
            { name: '田尾鄉', zipCode: '522' },
            { name: '埤頭鄉', zipCode: '523' },
            { name: '溪州鄉', zipCode: '524' }
        ]
    },
    {
        name: '南投縣',
        code: 'NTO',
        districts: [
            { name: '南投市', zipCode: '540' },
            { name: '埔里鎮', zipCode: '545' },
            { name: '草屯鎮', zipCode: '542' },
            { name: '竹山鎮', zipCode: '557' },
            { name: '集集鎮', zipCode: '552' },
            { name: '名間鄉', zipCode: '551' },
            { name: '鹿谷鄉', zipCode: '558' },
            { name: '中寮鄉', zipCode: '541' },
            { name: '魚池鄉', zipCode: '555' },
            { name: '國姓鄉', zipCode: '544' },
            { name: '水里鄉', zipCode: '553' },
            { name: '信義鄉', zipCode: '556' },
            { name: '仁愛鄉', zipCode: '546' }
        ]
    },
    {
        name: '雲林縣',
        code: 'YUN',
        districts: [
            { name: '斗六市', zipCode: '640' },
            { name: '斗南鎮', zipCode: '630' },
            { name: '虎尾鎮', zipCode: '632' },
            { name: '西螺鎮', zipCode: '648' },
            { name: '土庫鎮', zipCode: '633' },
            { name: '北港鎮', zipCode: '651' },
            { name: '古坑鄉', zipCode: '646' },
            { name: '大埤鄉', zipCode: '631' },
            { name: '莿桐鄉', zipCode: '647' },
            { name: '林內鄉', zipCode: '643' },
            { name: '二崙鄉', zipCode: '649' },
            { name: '崙背鄉', zipCode: '637' },
            { name: '麥寮鄉', zipCode: '638' },
            { name: '東勢鄉', zipCode: '635' },
            { name: '褒忠鄉', zipCode: '634' },
            { name: '台西鄉', zipCode: '636' },
            { name: '元長鄉', zipCode: '655' },
            { name: '四湖鄉', zipCode: '654' },
            { name: '口湖鄉', zipCode: '653' },
            { name: '水林鄉', zipCode: '652' }
        ]
    },
    {
        name: '嘉義市',
        code: 'CYI',
        districts: [
            { name: '東區', zipCode: '600' },
            { name: '西區', zipCode: '600' }
        ]
    },
    {
        name: '嘉義縣',
        code: 'CYQ',
        districts: [
            { name: '太保市', zipCode: '612' },
            { name: '朴子市', zipCode: '613' },
            { name: '布袋鎮', zipCode: '625' },
            { name: '大林鎮', zipCode: '622' },
            { name: '民雄鄉', zipCode: '621' },
            { name: '溪口鄉', zipCode: '623' },
            { name: '新港鄉', zipCode: '616' },
            { name: '六腳鄉', zipCode: '615' },
            { name: '東石鄉', zipCode: '614' },
            { name: '義竹鄉', zipCode: '624' },
            { name: '鹿草鄉', zipCode: '611' },
            { name: '水上鄉', zipCode: '608' },
            { name: '中埔鄉', zipCode: '606' },
            { name: '竹崎鄉', zipCode: '604' },
            { name: '梅山鄉', zipCode: '603' },
            { name: '番路鄉', zipCode: '602' },
            { name: '大埔鄉', zipCode: '607' },
            { name: '阿里山鄉', zipCode: '605' }
        ]
    },
    {
        name: '台南市',
        code: 'TNN',
        districts: [
            { name: '中西區', zipCode: '700' },
            { name: '東區', zipCode: '701' },
            { name: '南區', zipCode: '702' },
            { name: '北區', zipCode: '704' },
            { name: '安平區', zipCode: '708' },
            { name: '安南區', zipCode: '709' },
            { name: '永康區', zipCode: '710' },
            { name: '歸仁區', zipCode: '711' },
            { name: '新化區', zipCode: '712' },
            { name: '左鎮區', zipCode: '713' },
            { name: '玉井區', zipCode: '714' },
            { name: '楠西區', zipCode: '715' },
            { name: '南化區', zipCode: '716' },
            { name: '仁德區', zipCode: '717' },
            { name: '關廟區', zipCode: '718' },
            { name: '龍崎區', zipCode: '719' },
            { name: '官田區', zipCode: '720' },
            { name: '麻豆區', zipCode: '721' },
            { name: '佳里區', zipCode: '722' },
            { name: '西港區', zipCode: '723' },
            { name: '七股區', zipCode: '724' },
            { name: '將軍區', zipCode: '725' },
            { name: '學甲區', zipCode: '726' },
            { name: '北門區', zipCode: '727' },
            { name: '新營區', zipCode: '730' },
            { name: '後壁區', zipCode: '731' },
            { name: '白河區', zipCode: '732' },
            { name: '東山區', zipCode: '733' },
            { name: '六甲區', zipCode: '734' },
            { name: '下營區', zipCode: '735' },
            { name: '柳營區', zipCode: '736' },
            { name: '鹽水區', zipCode: '737' },
            { name: '善化區', zipCode: '741' },
            { name: '大內區', zipCode: '742' },
            { name: '山上區', zipCode: '743' },
            { name: '新市區', zipCode: '744' },
            { name: '安定區', zipCode: '745' }
        ]
    },
    {
        name: '高雄市',
        code: 'KHH',
        districts: [
            { name: '新興區', zipCode: '800' },
            { name: '前金區', zipCode: '801' },
            { name: '苓雅區', zipCode: '802' },
            { name: '鹽埕區', zipCode: '803' },
            { name: '鼓山區', zipCode: '804' },
            { name: '旗津區', zipCode: '805' },
            { name: '前鎮區', zipCode: '806' },
            { name: '三民區', zipCode: '807' },
            { name: '楠梓區', zipCode: '811' },
            { name: '小港區', zipCode: '812' },
            { name: '左營區', zipCode: '813' },
            { name: '仁武區', zipCode: '814' },
            { name: '大社區', zipCode: '815' },
            { name: '岡山區', zipCode: '820' },
            { name: '路竹區', zipCode: '821' },
            { name: '阿蓮區', zipCode: '822' },
            { name: '田寮區', zipCode: '823' },
            { name: '燕巢區', zipCode: '824' },
            { name: '橋頭區', zipCode: '825' },
            { name: '梓官區', zipCode: '826' },
            { name: '彌陀區', zipCode: '827' },
            { name: '永安區', zipCode: '828' },
            { name: '湖內區', zipCode: '829' },
            { name: '鳳山區', zipCode: '830' },
            { name: '大寮區', zipCode: '831' },
            { name: '林園區', zipCode: '832' },
            { name: '鳥松區', zipCode: '833' },
            { name: '大樹區', zipCode: '840' },
            { name: '旗山區', zipCode: '842' },
            { name: '美濃區', zipCode: '843' },
            { name: '六龜區', zipCode: '844' },
            { name: '內門區', zipCode: '845' },
            { name: '杉林區', zipCode: '846' },
            { name: '甲仙區', zipCode: '847' },
            { name: '桃源區', zipCode: '848' },
            { name: '那瑪夏區', zipCode: '849' },
            { name: '茂林區', zipCode: '851' },
            { name: '茄萣區', zipCode: '852' }
        ]
    },
    {
        name: '屏東縣',
        code: 'PIF',
        districts: [
            { name: '屏東市', zipCode: '900' },
            { name: '潮州鎮', zipCode: '920' },
            { name: '東港鎮', zipCode: '928' },
            { name: '恆春鎮', zipCode: '946' },
            { name: '萬丹鄉', zipCode: '913' },
            { name: '長治鄉', zipCode: '908' },
            { name: '麟洛鄉', zipCode: '909' },
            { name: '九如鄉', zipCode: '904' },
            { name: '里港鄉', zipCode: '905' },
            { name: '鹽埔鄉', zipCode: '907' },
            { name: '高樹鄉', zipCode: '906' },
            { name: '萬巒鄉', zipCode: '923' },
            { name: '內埔鄉', zipCode: '912' },
            { name: '竹田鄉', zipCode: '911' },
            { name: '新埤鄉', zipCode: '925' },
            { name: '枋寮鄉', zipCode: '940' },
            { name: '新園鄉', zipCode: '932' },
            { name: '崁頂鄉', zipCode: '924' },
            { name: '林邊鄉', zipCode: '927' },
            { name: '南州鄉', zipCode: '926' },
            { name: '佳冬鄉', zipCode: '931' },
            { name: '琉球鄉', zipCode: '929' },
            { name: '車城鄉', zipCode: '944' },
            { name: '滿州鄉', zipCode: '947' },
            { name: '枋山鄉', zipCode: '941' },
            { name: '三地門鄉', zipCode: '901' },
            { name: '霧臺鄉', zipCode: '902' },
            { name: '瑪家鄉', zipCode: '903' },
            { name: '泰武鄉', zipCode: '921' },
            { name: '來義鄉', zipCode: '922' },
            { name: '春日鄉', zipCode: '942' },
            { name: '獅子鄉', zipCode: '943' },
            { name: '牡丹鄉', zipCode: '945' }
        ]
    },
    {
        name: '宜蘭縣',
        code: 'ILA',
        districts: [
            { name: '宜蘭市', zipCode: '260' },
            { name: '羅東鎮', zipCode: '265' },
            { name: '蘇澳鎮', zipCode: '270' },
            { name: '頭城鎮', zipCode: '261' },
            { name: '礁溪鄉', zipCode: '262' },
            { name: '壯圍鄉', zipCode: '263' },
            { name: '員山鄉', zipCode: '264' },
            { name: '冬山鄉', zipCode: '269' },
            { name: '五結鄉', zipCode: '268' },
            { name: '三星鄉', zipCode: '266' },
            { name: '大同鄉', zipCode: '267' },
            { name: '南澳鄉', zipCode: '272' }
        ]
    },
    {
        name: '花蓮縣',
        code: 'HWA',
        districts: [
            { name: '花蓮市', zipCode: '970' },
            { name: '鳳林鎮', zipCode: '975' },
            { name: '玉里鎮', zipCode: '981' },
            { name: '新城鄉', zipCode: '971' },
            { name: '吉安鄉', zipCode: '973' },
            { name: '壽豐鄉', zipCode: '974' },
            { name: '光復鄉', zipCode: '976' },
            { name: '豐濱鄉', zipCode: '977' },
            { name: '瑞穗鄉', zipCode: '978' },
            { name: '富里鄉', zipCode: '983' },
            { name: '秀林鄉', zipCode: '972' },
            { name: '萬榮鄉', zipCode: '979' },
            { name: '卓溪鄉', zipCode: '982' }
        ]
    },
    {
        name: '台東縣',
        code: 'TTT',
        districts: [
            { name: '台東市', zipCode: '950' },
            { name: '成功鎮', zipCode: '961' },
            { name: '關山鎮', zipCode: '956' },
            { name: '卑南鄉', zipCode: '954' },
            { name: '鹿野鄉', zipCode: '955' },
            { name: '池上鄉', zipCode: '958' },
            { name: '東河鄉', zipCode: '959' },
            { name: '長濱鄉', zipCode: '962' },
            { name: '太麻里鄉', zipCode: '963' },
            { name: '大武鄉', zipCode: '965' },
            { name: '綠島鄉', zipCode: '951' },
            { name: '海端鄉', zipCode: '957' },
            { name: '延平鄉', zipCode: '953' },
            { name: '金峰鄉', zipCode: '964' },
            { name: '達仁鄉', zipCode: '966' },
            { name: '蘭嶼鄉', zipCode: '952' }
        ]
    },
    {
        name: '澎湖縣',
        code: 'PEH',
        districts: [
            { name: '馬公市', zipCode: '880' },
            { name: '湖西鄉', zipCode: '885' },
            { name: '白沙鄉', zipCode: '884' },
            { name: '西嶼鄉', zipCode: '881' },
            { name: '望安鄉', zipCode: '882' },
            { name: '七美鄉', zipCode: '883' }
        ]
    },
    {
        name: '金門縣',
        code: 'KMN',
        districts: [
            { name: '金城鎮', zipCode: '893' },
            { name: '金湖鎮', zipCode: '891' },
            { name: '金沙鎮', zipCode: '890' },
            { name: '金寧鄉', zipCode: '892' },
            { name: '烈嶼鄉', zipCode: '894' },
            { name: '烏坵鄉', zipCode: '896' }
        ]
    },
    {
        name: '連江縣',
        code: 'LNN',
        districts: [
            { name: '南竿鄉', zipCode: '209' },
            { name: '北竿鄉', zipCode: '210' },
            { name: '莒光鄉', zipCode: '211' },
            { name: '東引鄉', zipCode: '212' }
        ]
    }
];

const props = defineProps<{
    modelValue: string | null;
    invalid?: boolean;
}>();

const emit = defineEmits<{
    'update:model-value': [value: string];
}>();

const selectedCity = ref<City | null>(null);
const selectedDistrict = ref<District | null>(null);
const inputZipCode = ref<string>('');

const hasInput = computed(() => inputZipCode.value !== '');

// 判斷郵遞區號是否存在於資料庫中
const isZipCodeValid = computed(() => {
    const currentZipCode = inputZipCode.value;
    if (!currentZipCode) return true;
    return cities.some(city =>
        city.districts.some(district =>
            district.zipCode === currentZipCode
        )
    );
});

// 當選擇城市改變時，重置區域選擇，除非是初始化時的設定
watch(selectedCity, (newCity, oldCity) => {
    // 如果是初始化時的設定，不要重置區域
    if (!oldCity) return;
    selectedDistrict.value = null;
    updateValue();
});

// 當選擇區域改變時，更新值
watch(selectedDistrict, () => {
    if (selectedDistrict.value) {
        inputZipCode.value = selectedDistrict.value.zipCode;
    }
    updateValue();
});

// 監聽輸入的郵遞區號
watch(inputZipCode, (newValue) => {
    if (newValue) {
        let found = false;
        // 尋找對應的城市和地區
        for (const city of cities) {
            const district = city.districts.find(d => d.zipCode === newValue);
            if (district) {
                selectedCity.value = city;
                selectedDistrict.value = district;
                found = true;
                break;
            }
        }
        // 如果找不到對應的郵遞區號，清空選擇
        if (!found) {
            selectedCity.value = null;
            selectedDistrict.value = null;
        }
    }
    emit('update:model-value', newValue);
});

// 更新值並發出事件
function updateValue() {
    const zipCode = selectedDistrict.value?.zipCode || inputZipCode.value || '';
    emit('update:model-value', zipCode);
}

// 當外部值改變時，更新選擇的城市和地區
watch(() => props.modelValue, (newValue) => {
    inputZipCode.value = newValue || '';

    if (!newValue) {
        selectedCity.value = null;
        selectedDistrict.value = null;
        return;
    }

    // 尋找對應的城市和地區
    for (const city of cities) {
        const district = city.districts.find(d => d.zipCode === newValue);
        if (district) {
            selectedCity.value = city;
            selectedDistrict.value = district;
            break;
        }
    }
}, { immediate: true });
</script>

<template>
    <div class="grid grid-cols-12 gap-2">
        <InputText v-model="inputZipCode" placeholder="號碼" class="w-full col-span-3"
            :class="{ 'p-invalid': invalid }" />
        <div class="col-span-9 grid grid-cols-2 gap-2">
            <Select v-model="selectedCity" :options="cities" optionLabel="name" placeholder="縣市" class="w-full"
                :class="{ 'p-invalid': !isZipCodeValid && hasInput }" />
            <Select v-model="selectedDistrict" :options="selectedCity?.districts || []" optionLabel="name"
                placeholder="區域" class="w-full" :disabled="!selectedCity"
                :class="{ 'p-invalid': !isZipCodeValid && hasInput }" />
        </div>
    </div>
</template>