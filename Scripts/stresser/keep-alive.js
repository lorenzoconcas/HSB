import http from 'k6/http'

export const options = {
  vus: 1000,
  duration: '60s',
}

export default function () {
  for(let i=0;i<100;i++) {
    http.get('http://127.0.0.1:8080/health')
  }
}
