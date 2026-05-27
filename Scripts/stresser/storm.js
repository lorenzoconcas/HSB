import http from 'k6/http'

export const options = {
  noConnectionReuse: true,
  vus: 500,
  duration: '30s',
}

export default function () {
  http.get('http://127.0.0.1:8080/health')
}
